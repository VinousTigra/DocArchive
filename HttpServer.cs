#nullable enable

using System.Net;
using System.Net.Sockets;
using DocArhive.Models;

namespace DocArhive;

public class HttpServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly Router _router;
    private readonly HttpServerOptions _options;
    private readonly SemaphoreSlim _connectionSemaphore;
    private readonly CancellationTokenSource _serverCts = new();
    private Task _serverTask = Task.CompletedTask;

    public HttpServer(HttpServerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _listener = new TcpListener(IPAddress.Parse(options.IpAddress), options.Port);
        _router = new Router(new ProjectState());
        _connectionSemaphore = new SemaphoreSlim(options.MaxConcurrentConnections);
    }

    public void Start()
    {
        _listener.Start();
        var endpoint = (IPEndPoint)_listener.LocalEndpoint;
        Console.WriteLine($"[INFO] Сервер запущен на http://{endpoint.Address}:{endpoint.Port}");
        Console.WriteLine($"[INFO] Макс. параллельных подключений: {_options.MaxConcurrentConnections}");

        _serverTask = Task.Run(RunAcceptLoopAsync);
    }

    public async Task StopAsync()
    {
        Console.WriteLine("[INFO] Остановка сервера...");
        _serverCts.Cancel();
        _listener.Stop();
        await _serverTask;
        _connectionSemaphore.Dispose();
        Console.WriteLine("[INFO] Сервер остановлен");
    }

    private async Task RunAcceptLoopAsync()
    {
        try
        {
            while (!_serverCts.Token.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync(_serverCts.Token);
                    
                    client.Client.ReceiveTimeout = _options.ReceiveTimeoutMs;
                    client.Client.SendTimeout = _options.SendTimeoutMs;

                    await _connectionSemaphore.WaitAsync(_serverCts.Token);

                    _ = Task.Run(() => ProcessClientAsync(client, _serverCts.Token))
                            .ContinueWith(t =>
                            {
                                _connectionSemaphore.Release();
                                if (t.IsFaulted && t.Exception != null)
                                {
                                    Console.Error.WriteLine($"[ERROR] Необработанное исключение: {t.Exception.InnerException?.Message}");
                                }
                            }, TaskContinuationOptions.ExecuteSynchronously);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException) when (_serverCts.IsCancellationRequested)
                {
                    // Listener остановлен, выходим
                    break;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[ERROR] Ошибка принятия подключения: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[FATAL] Ошибка в цикле приёма: {ex.Message}");
        }
    }

    private async Task ProcessClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        {
            var stream = client.GetStream();
            try
            {
                // 1. Парсим запрос напрямую из NetworkStream
                var request = await HttpParser.ReadHttpRequestAsync(stream, _options, cancellationToken);
                if (request == null)
                    return; // клиент закрыл соединение или сервер останавливается

                Console.WriteLine($"{DateTime.Now:HH:mm:ss} - {request.Method} {request.Path}");

                // 2. Маршрутизация (синхронная, но может быть заменена на асинхронную)
                var response = _router.Route(request);

                // 3. Отправка ответа
                await HttpParser.SendResponseAsync(stream, response, cancellationToken);
            }
            catch (HttpRequestException ex) when (ex.IsTimeout)
            {
                await HttpParser.SendResponseAsync(stream, HttpResponse.Timeout(), cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"[WARN] Некорректный запрос: {ex.Message}");
                await HttpParser.SendResponseAsync(stream, HttpResponse.BadRequest(ex.Message), cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Сервер останавливается – просто выходим без ответа
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] Внутренняя ошибка: {ex.Message}");
                await HttpParser.SendResponseAsync(stream, HttpResponse.InternalServerError(), cancellationToken);
            }
        }
    }

    public void Dispose()
    {
        _serverCts?.Cancel();
        _serverCts?.Dispose();
        _connectionSemaphore?.Dispose();
        _listener?.Stop();
    }
}