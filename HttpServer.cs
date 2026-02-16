using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DocArhive.Models;
using Microsoft.Extensions.Logging;

namespace DocArhive;

public class HttpServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly Router _router;
    private readonly HttpServerOptions _options;
    private readonly SemaphoreSlim _connectionSemaphore;
    private readonly CancellationTokenSource _serverCts = new();
    private readonly ILogger<HttpServer> _logger;
    private Task _serverTask = Task.CompletedTask;

    public HttpServer(HttpServerOptions options, ILogger<HttpServer> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _listener = new TcpListener(IPAddress.Parse(options.IpAddress), options.Port);
        _router = new Router(new ProjectState());
        _connectionSemaphore = new SemaphoreSlim(options.MaxConcurrentConnections);
    }

    public void Start()
    {
        _listener.Start();
        var endpoint = (IPEndPoint)_listener.LocalEndpoint;
        _logger.LogInformation("Сервер запущен на http://{Address}:{Port}", endpoint.Address, endpoint.Port);
        _logger.LogInformation("Макс. параллельных подключений: {Max}", _options.MaxConcurrentConnections);

        _serverTask = Task.Run(RunAcceptLoopAsync);
    }

    public async Task StopAsync()
    {
        _logger.LogInformation("Остановка сервера...");
        _serverCts.Cancel();
        _listener.Stop();
        await _serverTask;
        _connectionSemaphore.Dispose();
        _logger.LogInformation("Сервер остановлен");
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
                                _logger.LogError(t.Exception, "Необработанное исключение при обработке клиента");
                            }
                        }, TaskContinuationOptions.ExecuteSynchronously);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException) when (_serverCts.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка принятия подключения");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Ошибка в цикле приёма");
        }
    }

    private async Task ProcessClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        {
            var stream = client.GetStream();
            bool keepAlive = true;

            while (keepAlive && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Таймаут ожидания следующего запроса на этом соединении
                    using var timeoutCts = new CancellationTokenSource(_options.KeepAliveTimeoutMs);
                    using var linkedCts =
                        CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                    var token = linkedCts.Token;

                    var request = await HttpParser.ReadHttpRequestAsync(stream, _options, token);
                    if (request == null)
                        break; // клиент закрыл соединение

                    _logger.LogInformation("{Time:HH:mm:ss} - {Method} {Path}", DateTime.Now, request.Method,
                        request.Path);
                    var response = await _router.RouteAsync(request);

                    // Определяем, нужно ли закрыть соединение
                    if (request.Headers.TryGetValue("Connection", out var reqConn) &&
                        reqConn.Equals("close", StringComparison.OrdinalIgnoreCase))
                    {
                        keepAlive = false;
                    }

                    if (!response.KeepAlive) keepAlive = false;

                    await HttpParser.SendResponseAsync(stream, response, token, keepAlive);
                }
                catch (HttpRequestException ex) when (ex.IsTimeout)
                {
                    _logger.LogWarning("Таймаут ожидания следующего запроса");
                    await HttpParser.SendResponseAsync(stream, HttpResponse.Timeout(), cancellationToken, false);
                    break;
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning(ex, "Некорректный запрос");
                    await HttpParser.SendResponseAsync(stream, HttpResponse.BadRequest(ex.Message), cancellationToken,
                        false);
                    break;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // сервер останавливается
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Внутренняя ошибка");
                    await HttpParser.SendResponseAsync(stream, HttpResponse.InternalServerError(), cancellationToken);
                    break;
                }
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