namespace DocArhive;

using System.Net;
using System.Net.Sockets;
using System.Text;
using Models;

public class HttpServer
{
    private readonly TcpListener _listener;
    private readonly Router _router;
    private bool _isRunning;

    public HttpServer(string ipAddress, int port)
    {
        _listener = new TcpListener(IPAddress.Parse(ipAddress), port);
        var projectState = new ProjectState();
        _router = new Router(projectState);
    }

    public void Start()
    {
        _isRunning = true;
        _listener.Start();
        var endpoint = (IPEndPoint)_listener.LocalEndpoint;
        Console.WriteLine($"Сервер запущен на http://{endpoint.Address}:{endpoint.Port}");

        while (_isRunning)
        {
            try
            {
                var client = _listener.AcceptTcpClient();
                Task.Run(() => ProcessClient(client));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при принятии подключения: {ex.Message}");
            }
        }
    }

    public void Stop()
    {
        _isRunning = false;
        _listener.Stop();
    }

    private async Task ProcessClient(TcpClient client)
    {
        using (client)
        using (var stream = client.GetStream())
        using (var reader = new StreamReader(stream, Encoding.UTF8))
        {
            try
            {
                // Читаем HTTP-запрос
                var request = await ReadHttpRequestAsync(reader);

                if (request != null)
                {
                    // Выводим запрос в консоль для отладки
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss} - {request.Method} {request.Path}");

                    // Обрабатываем запрос через маршрутизатор
                    var response = _router.Route(request);

                    // Отправляем ответ
                    await SendResponseAsync(stream, response);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки клиента: {ex.Message}");
            }
        }
    }

    private static async Task<HttpRequest?> ReadHttpRequestAsync(StreamReader reader)
    {
        try
        {
            // Читаем первую строку запроса
            var firstLine = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(firstLine))
                return null;

            var parts = firstLine.Split(' ');
            if (parts.Length < 3)
                return null;

            var method = parts[0];
            var path = parts[1];

            // Читаем заголовки
            var headers = new Dictionary<string, string>();
            string? line;
            while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
            {
                var separatorIndex = line.IndexOf(':');
                if (separatorIndex > 0)
                {
                    var key = line[..separatorIndex].Trim();
                    var value = line[(separatorIndex + 1)..].Trim();
                    headers[key] = value;
                }
            }

            // Читаем тело запроса (если есть)
            var body = "";
            if (headers.TryGetValue("Content-Length", out var contentLengthStr))
            {
                var contentLength = int.Parse(contentLengthStr);
                var buffer = new char[contentLength];
                await reader.ReadAsync(buffer.AsMemory(0, contentLength));
                body = new string(buffer);
            }

            return new HttpRequest
            {
                Method = method,
                Path = path,
                Headers = headers,
                Body = body
            };
        }
        catch
        {
            return null;
        }
    }

    private static async Task SendResponseAsync(NetworkStream stream, HttpResponse response)
    {
        await using var writer = new StreamWriter(stream, Encoding.UTF8);
        writer.AutoFlush = true;

        // Статусная строка
        await writer.WriteLineAsync($"HTTP/1.1 {response.StatusCode} {response.StatusMessage}");

        // Заголовки
        await writer.WriteLineAsync($"Content-Type: {response.ContentType}");
        await writer.WriteLineAsync($"Content-Length: {Encoding.UTF8.GetByteCount(response.Content)}");
        await writer.WriteLineAsync("Connection: close");
        await writer.WriteLineAsync();

        // Тело ответа
        await writer.WriteAsync(response.Content);
    }
}

public class HttpRequest
{
    public string Method { get; init; } = "";
    public string Path { get; init; } = "";
    public Dictionary<string, string> Headers { get; init; } = new();
    public string Body { get; init; } = "";
}

public class HttpResponse
{
    public int StatusCode { get; init; } = 200;
    public string StatusMessage { get; init; } = "OK";
    public string ContentType => "text/html; charset=utf-8";
    public string Content { get; init; } = "";
}