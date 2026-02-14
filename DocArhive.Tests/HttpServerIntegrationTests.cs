/*namespace DocArhive.Tests.Integration;

using System.Net.Sockets;
using System.Text;
using DocArhive;
using FluentAssertions;
using Xunit;


public class HttpServerIntegrationTests : IAsyncLifetime, IDisposable
{
    private HttpServer? _server;
    private readonly HttpServerOptions _options;
    private readonly CancellationTokenSource _cts = new();

    public HttpServerIntegrationTests()
    {
        _options = new HttpServerOptions
        {
            IpAddress = "127.0.0.1",
            Port = 8081,
            MaxConcurrentConnections = 10,
            MaxRequestSize = 1024 * 1024,
            MaxHeaderSize = 16 * 1024,
            ReceiveTimeoutMs = 5000,
            SendTimeoutMs = 5000
        };
    }

    public async Task InitializeAsync()
    {
        _server = new HttpServer(_options);
        _server.Start();

        // Ждём, пока сервер действительно запустится (проверяем подключение с ретраями)
        var maxAttempts = 10;
        var delay = 200;
        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(_options.IpAddress, _options.Port, _cts.Token);
                // Если достучались – сервер готов
                return;
            }
            catch
            {
                await Task.Delay(delay);
            }
        }

        throw new InvalidOperationException("Сервер не запустился за отведённое время");
    }

    public async Task DisposeAsync()
    {
        _cts.Cancel();
        if (_server != null)
        {
            await _server.StopAsync();
            _server.Dispose();
        }
    }

    public void Dispose()
    {
        _cts?.Dispose();
    }

    [Fact]
    public async Task Server_ShouldRespondToGetRequest()
    {
        using var client = new TcpClient();
        await client.ConnectAsync(_options.IpAddress, _options.Port, _cts.Token);

        await using var stream = client.GetStream();
        await using var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };
        using var reader = new StreamReader(stream, Encoding.UTF8);

        await writer.WriteAsync("GET / HTTP/1.1\r\n");
        await writer.WriteAsync($"Host: {_options.IpAddress}:{_options.Port}\r\n");
        await writer.WriteAsync("Connection: close\r\n");
        await writer.WriteAsync("\r\n");

        var firstLine = await reader.ReadLineAsync(_cts.Token);
        firstLine.Should().NotBeNullOrEmpty();
        firstLine.Should().Contain("200 OK");
    }

    [Fact]
    public async Task Server_ShouldHandleParallelRequests()
    {
        var tasks = new List<Task>();
        var successes = 0;
        var failures = 0;

        for (int i = 0; i < 5; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    using var client = new TcpClient();
                    await client.ConnectAsync(_options.IpAddress, _options.Port, _cts.Token);

                    await using var stream = client.GetStream();
                    await using var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };
                    await using var reader = new StreamReader(stream, Encoding.UTF8);

                    await writer.WriteAsync("GET / HTTP/1.1\r\n");
                    await writer.WriteAsync($"Host: {_options.IpAddress}:{_options.Port}\r\n");
                    await writer.WriteAsync("Connection: close\r\n");
                    await writer.WriteAsync("\r\n");

                    var firstLine = await reader.ReadLineAsync(_cts.Token);
                    if (!string.IsNullOrEmpty(firstLine) && firstLine.Contains("200 OK"))
                    {
                        Interlocked.Increment(ref successes);
                    }
                    else
                    {
                        Interlocked.Increment(ref failures);
                    }
                }
                catch
                {
                    Interlocked.Increment(ref failures);
                }
            }, _cts.Token));
        }

        await Task.WhenAll(tasks);

        successes.Should().BeGreaterThan(0);
        failures.Should().Be(0); // при правильной работе – ни одного отказа
    }

    [Fact]
    public void Server_WithInvalidPort_ShouldThrowException()
    {
        // Act
        var act = () =>
        {
            var options = new HttpServerOptions
            {
                IpAddress = "127.0.0.1",
                Port = 99999 // невалидный порт (>65535)
            };
            _ = new HttpServer(options); // исключение произойдёт внутри Start, но TcpListener проверяет порт при создании?
        };

        // Assert: TcpListener выбросит ArgumentOutOfRangeException при создании, если порт вне диапазона
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task Server_ShouldReturn404ForUnknownPath()
    {
        using var client = new TcpClient();
        await client.ConnectAsync(_options.IpAddress, _options.Port, _cts.Token);

        await using var stream = client.GetStream();
        await using var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };
        await using var reader = new StreamReader(stream, Encoding.UTF8);

        await writer.WriteAsync("GET /non-existent-page HTTP/1.1\r\n");
        await writer.WriteAsync($"Host: {_options.IpAddress}:{_options.Port}\r\n");
        await writer.WriteAsync("Connection: close\r\n");
        await writer.WriteAsync("\r\n");

        var firstLine = await reader.ReadLineAsync(_cts.Token);
        firstLine.Should().Contain("404 Not Found");
    }

    [Fact]
    public async Task Server_ShouldProcessPostRequest()
    {
        using var client = new TcpClient();
        await client.ConnectAsync(_options.IpAddress, _options.Port, _cts.Token);

        await using var stream = client.GetStream();
        await using var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };
        await using var reader = new StreamReader(stream, Encoding.UTF8);

        // Тело запроса (формат application/x-www-form-urlencoded)
        var body = "title=TestDocument&description=IntegrationTest&filename=test.pdf&category=Test";
        var contentLength = Encoding.UTF8.GetByteCount(body);

        await writer.WriteAsync("POST /action HTTP/1.1\r\n");
        await writer.WriteAsync($"Host: {_options.IpAddress}:{_options.Port}\r\n");
        await writer.WriteAsync("Content-Type: application/x-www-form-urlencoded\r\n");
        await writer.WriteAsync($"Content-Length: {contentLength}\r\n");
        await writer.WriteAsync("Connection: close\r\n");
        await writer.WriteAsync("\r\n");
        await writer.WriteAsync(body);

        var firstLine = await reader.ReadLineAsync(_cts.Token);
        firstLine.Should().Contain("200 OK");

        // Читаем остаток ответа (должен содержать alert об успехе)
        var responseBody = await reader.ReadToEndAsync();
        responseBody.Should().Contain("Документ успешно добавлен");
    }
}*/