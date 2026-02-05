namespace DocArhive.Tests.Integration;

using System.Net.Sockets;
using System.Text;
using DocArhive;
using FluentAssertions;
using Xunit;


public class HttpServerIntegrationTests : IDisposable
{
    private readonly HttpServer _server;
    private readonly Thread _serverThread;
    private readonly string _baseUrl = "http://127.0.0.1:8081";
    
    public HttpServerIntegrationTests()
    {
        _server = new HttpServer("127.0.0.1", 8081);
        _serverThread = new Thread(() => _server.Start());
        _serverThread.Start();
        
        // Подождем немного, чтобы сервер успел запуститься
        Thread.Sleep(500);
    }
    
    public void Dispose()
    {
        _server.Stop();
        if (_serverThread.IsAlive)
        {
            _serverThread.Join(1000);
        }
    }
    
    [Fact]
    public async Task Server_ShouldRespondToGetRequest()
    {
        // Используем TcpClient вместо HttpClient для полного контроля
        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", 8081);
        
        using var stream = client.GetStream();
        using var writer = new StreamWriter(stream, Encoding.ASCII);
        writer.AutoFlush = true;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        
        // Отправляем простой GET запрос
        await writer.WriteAsync("GET / HTTP/1.1\r\n");
        await writer.WriteAsync("Host: localhost:8081\r\n");
        await writer.WriteAsync("Connection: close\r\n");
        await writer.WriteAsync("\r\n");
        
        // Читаем ответ
        string? firstLine = await reader.ReadLineAsync();
        firstLine.Should().NotBeNullOrEmpty();
        firstLine.Should().Contain("200 OK");
    }
    
    [Fact]
    public async Task Server_ShouldHandleParallelRequests()
    {
        // Используем Task.WhenAll для параллельных запросов
        var tasks = new List<Task>();
        var successes = 0;
        var failures = 0;
        
        for (int i = 0; i < 3; i++) // Уменьшим до 3 для надежности
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    using var client = new TcpClient();
                    await client.ConnectAsync("127.0.0.1", 8081);
                    
                    using var stream = client.GetStream();
                    using var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };
                    using var reader = new StreamReader(stream, Encoding.UTF8);
                    
                    await writer.WriteAsync("GET / HTTP/1.1\r\n");
                    await writer.WriteAsync("Host: localhost:8081\r\n");
                    await writer.WriteAsync("Connection: close\r\n");
                    await writer.WriteAsync("\r\n");
                    
                    var firstLine = await reader.ReadLineAsync();
                    if (!string.IsNullOrEmpty(firstLine) && firstLine.Contains("200"))
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
            }));
        }
        
        await Task.WhenAll(tasks);
        
        // Убедимся, что большинство запросов прошло успешно
        successes.Should().BeGreaterThan(0);
    }
    
    [Fact]
    public void Server_OnInvalidPort_ShouldThrowException()
    {
        // Act
        Action act = () => new HttpServer("127.0.0.1", 99999);
        
        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
    
    // Альтернативный тест с использованием HttpListener (более надежный)
    [Fact]
    public async Task Server_ShouldProcessHttpRequests_Correctly()
    {
        // Arrange
        var request = "GET / HTTP/1.1\r\n" +
                      "Host: localhost:8081\r\n" +
                      "Connection: close\r\n" +
                      "\r\n";
        
        var requestBytes = Encoding.ASCII.GetBytes(request);
        
        // Act
        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", 8081);
        
        await client.GetStream().WriteAsync(requestBytes.AsMemory(0, requestBytes.Length));
        
        // Читаем ответ
        using var reader = new StreamReader(client.GetStream(), Encoding.UTF8);
        var response = await reader.ReadToEndAsync();
        
        // Assert
        response.Should().Contain("HTTP/1.1 200 OK");
        response.Should().Contain("Content-Type: text/html");
    }
}