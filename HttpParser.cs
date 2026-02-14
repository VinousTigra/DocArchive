#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DocArhive;

public static class HttpParser
{
    /// <summary>
    /// Читает HTTP-запрос из потока напрямую (без StreamReader), чтобы корректно обрабатывать тело в байтах.
    /// Поддерживает Content-Length, таймауты, настраиваемые лимиты.
    /// </summary>
    public static async Task<HttpRequest?> ReadHttpRequestAsync(
        NetworkStream stream,
        HttpServerOptions options,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource();
        timeoutCts.CancelAfter(options.ReceiveTimeoutMs);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        var combinedToken = linkedCts.Token;

        try
        {
            // 1. Читаем стартовую строку (до \r\n)
            var firstLine = await ReadLineAsync(stream, options.MaxHeaderSize, combinedToken);
            if (firstLine == null)
                return null; // клиент закрыл соединение

            var parts = firstLine.Split(' ');
            if (parts.Length != 3)
                throw new HttpRequestException("Некорректная стартовая строка");

            var method = parts[0];
            var path = parts[1];
            // version = parts[2]; – игнорируем

            // 2. Читаем заголовки до пустой строки
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            int totalHeaderSize = 0;
            while (totalHeaderSize < options.MaxHeaderSize)
            {
                var line = await ReadLineAsync(stream, options.MaxHeaderSize - totalHeaderSize, combinedToken);
                if (line == null)
                    throw new HttpRequestException("Неожиданный конец заголовков");
                totalHeaderSize += line.Length + 2; // + CRLF (в байтах – приблизительно)

                if (string.IsNullOrEmpty(line))
                    break; // пустая строка – конец заголовков

                var colonIndex = line.IndexOf(':');
                if (colonIndex <= 0)
                    continue; // игнорируем некорректные заголовки

                var key = line.Substring(0, colonIndex).Trim();
                var value = line.Substring(colonIndex + 1).Trim();
                headers[key] = value;
            }

            if (headers.Count > options.MaxHeaderCount)
                throw new HttpRequestException("Превышено максимальное количество заголовков");

            // 3. Чтение тела запроса
            string body = "";

            // 3.1 Content-Length
            if (headers.TryGetValue("Content-Length", out var contentLengthStr))
            {
                if (!int.TryParse(contentLengthStr, out var contentLength) || contentLength < 0)
                    throw new HttpRequestException("Некорректный Content-Length");

                if (contentLength > options.MaxRequestSize)
                    throw new HttpRequestException("Размер запроса превышает допустимый");

                if (contentLength > 0)
                {
                    var bodyBytes = new byte[contentLength];
                    int read = 0;
                    while (read < contentLength)
                    {
                        var bytesRead = await stream.ReadAsync(bodyBytes.AsMemory(read, contentLength - read), combinedToken);
                        if (bytesRead == 0)
                            throw new HttpRequestException("Прервано соединение при чтении тела");
                        read += bytesRead;
                    }
                    body = Encoding.UTF8.GetString(bodyBytes);
                }
            }
            // 3.2 Transfer-Encoding: chunked (базовая реализация)
            else if (headers.TryGetValue("Transfer-Encoding", out var transferEncoding) &&
                     transferEncoding.Contains("chunked", StringComparison.OrdinalIgnoreCase))
            {
                body = await ReadChunkedBodyAsync(stream, options.MaxRequestSize, combinedToken);
            }

            return new HttpRequest
            {
                Method = method,
                Path = path,
                Headers = headers,
                Body = body
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Сервер останавливается – просто выходим без ошибки
            return null;
        }
        catch (TimeoutException)
        {
            throw new HttpRequestException("408 Request Timeout", true); // специальный флаг для 408
        }
    }

    private static async Task<string?> ReadLineAsync(NetworkStream stream, int maxBytes, CancellationToken cancellationToken)
    {
        var buffer = new byte[1];
        var lineBytes = new List<byte>();
        bool crFound = false;

        while (lineBytes.Count < maxBytes)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(0, 1), cancellationToken);
            if (read == 0) return null; // конец потока

            byte b = buffer[0];
            if (b == '\r')
            {
                crFound = true;
                continue;
            }
            if (b == '\n' && crFound)
            {
                // конец строки, возвращаем без \r\n
                return Encoding.ASCII.GetString(lineBytes.ToArray());
            }
            if (crFound)
            {
                // если был \r, но следующий не \n – добавляем \r и текущий символ
                lineBytes.Add((byte)'\r');
                lineBytes.Add(b);
                crFound = false;
            }
            else
            {
                lineBytes.Add(b);
            }
        }

        throw new HttpRequestException("Превышен максимальный размер строки");
    }

    private static async Task<string> ReadChunkedBodyAsync(NetworkStream stream, int maxSize, CancellationToken cancellationToken)
    {
        // Упрощённая реализация для учебных целей
        // В продакшне используйте готовый парсер
        var body = new List<byte>();
        while (true)
        {
            var line = await ReadLineAsync(stream, 1024, cancellationToken);
            if (line == null)
                throw new HttpRequestException("Неожиданный конец потока при чтении chunked");

            var chunkSizeStr = line.Split(';')[0]; // игнорируем расширения
            if (!int.TryParse(chunkSizeStr, System.Globalization.NumberStyles.HexNumber, null, out var chunkSize))
                throw new HttpRequestException("Некорректный размер чанка");

            if (chunkSize == 0)
            {
                // конец чанков, читаем трейлеры (игнорируем)
                while (!string.IsNullOrEmpty(await ReadLineAsync(stream, 1024, cancellationToken))) { }
                break;
            }

            if (body.Count + chunkSize > maxSize)
                throw new HttpRequestException("Превышен максимальный размер тела");

            var chunk = new byte[chunkSize];
            int read = 0;
            while (read < chunkSize)
            {
                int bytesRead = await stream.ReadAsync(chunk.AsMemory(read, chunkSize - read), cancellationToken);
                if (bytesRead == 0)
                    throw new HttpRequestException("Прервано соединение при чтении чанка");
                read += bytesRead;
            }
            body.AddRange(chunk);

            // читаем \r\n после чанка
            await ReadLineAsync(stream, 2, cancellationToken);
        }

        return Encoding.UTF8.GetString(body.ToArray());
    }

    public static async Task SendResponseAsync(
        NetworkStream stream,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var responseBytes = Encoding.UTF8.GetBytes(
            $"HTTP/1.1 {response.StatusCode} {response.StatusMessage}\r\n" +
            $"Content-Type: {response.ContentType}\r\n" +
            $"Content-Length: {Encoding.UTF8.GetByteCount(response.Content)}\r\n" +
            "Connection: close\r\n" +
            "\r\n" +
            response.Content
        );

        await stream.WriteAsync(responseBytes.AsMemory(0, responseBytes.Length), cancellationToken);
    }
}

public class HttpRequestException : Exception
{
    public bool IsTimeout { get; }

    public HttpRequestException(string message, bool isTimeout = false) : base(message)
    {
        IsTimeout = isTimeout;
    }

    public HttpRequestException(string message, Exception inner, bool isTimeout = false) : base(message, inner)
    {
        IsTimeout = isTimeout;
    }
}