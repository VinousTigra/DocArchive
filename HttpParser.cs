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
    private enum ParseState
    {
        Method,
        Path,
        Version,
        HeaderKey,
        HeaderValue,
        HeaderCr,
        HeaderLf,
        Body,
        ChunkSize,
        ChunkData,
        ChunkCr,
        ChunkLf,
        Trailer,
        Done
    }

    private const int MaxTokenLength = 1024;
    private const int MaxUriLength = 2048;

    /// <summary>
    /// Читает HTTP-запрос из потока, используя конечный автомат и буферизованное чтение.
    /// </summary>
    public static async Task<HttpRequest?> ReadHttpRequestAsync(
        NetworkStream stream,
        HttpServerOptions options,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        var request = new HttpRequest();
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var state = ParseState.Method;
        var currentToken = new StringBuilder(128);
        string? headerKey = null;
        int contentLength = -1;
        int totalHeaderSize = 0;
        int totalBodySize = 0;
        var bodyBuffer = new MemoryStream();

        int chunkSize = 0;
        int chunkBytesRead = 0;

        using var timeoutCts = new CancellationTokenSource(options.ReceiveTimeoutMs);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        var combinedToken = linkedCts.Token;

        int offset = 0, count = 0;

        try
        {
            while (state != ParseState.Done && !combinedToken.IsCancellationRequested)
            {
                if (offset >= count)
                {
                    count = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), combinedToken);
                    if (count == 0)
                    {
                        // Если мы ещё не прочитали ни одного байта для этого запроса – клиент закрыл соединение
                        if (offset == 0 && state == ParseState.Method && currentToken.Length == 0)
                            return null; // нормальное завершение keep-alive

                        // Иначе – обрыв во время передачи данных
                        throw new HttpRequestException("Неожиданный конец потока");
                    }

                    offset = 0;
                }

                while (offset < count && state != ParseState.Done)
                {
                    byte b = buffer[offset++];
                    char ch = (char)b;

                    if (currentToken.Length > MaxTokenLength)
                        throw new HttpRequestException($"Слишком длинный токен (> {MaxTokenLength})");

                    switch (state)
                    {
                        case ParseState.Method:
                            if (ch == ' ')
                            {
                                request.Method = currentToken.ToString();
                                currentToken.Clear();
                                state = ParseState.Path;
                            }
                            else if (char.IsLetter(ch))
                            {
                                currentToken.Append(ch);
                            }
                            else
                            {
                                throw new HttpRequestException("Некорректный символ в методе");
                            }

                            break;

                        case ParseState.Path:
                            if (ch == ' ')
                            {
                                if (currentToken.Length > MaxUriLength)
                                    throw new HttpRequestException("414 URI Too Long");
                                request.Path = currentToken.ToString();
                                currentToken.Clear();
                                state = ParseState.Version;
                            }
                            else
                            {
                                currentToken.Append(ch);
                            }

                            break;

                        case ParseState.Version:
                            if (ch == '\r')
                            {
                                // ждём \n
                            }
                            else if (ch == '\n')
                            {
                                currentToken.Clear();
                                state = ParseState.HeaderKey;
                            }
                            else
                            {
                                currentToken.Append(ch);
                            }

                            break;

                        case ParseState.HeaderKey:
                            if (ch == ':')
                            {
                                headerKey = currentToken.ToString().Trim();
                                currentToken.Clear();
                                state = ParseState.HeaderValue;
                            }
                            else if (ch == '\r')
                            {
                                currentToken.Clear();
                                state = ParseState.Body;
                            }
                            else if (ch == '\n')
                            {
                                // игнорируем
                            }
                            else
                            {
                                currentToken.Append(ch);
                            }

                            break;

                        case ParseState.HeaderValue:
                            if (ch == '\r')
                            {
                                var value = currentToken.ToString().Trim();
                                currentToken.Clear();

                                if (headers.Count >= options.MaxHeaderCount)
                                    throw new HttpRequestException("431 Request Header Fields Too Large");

                                totalHeaderSize += headerKey!.Length + value.Length + 2;
                                if (totalHeaderSize > options.MaxHeaderSize)
                                    throw new HttpRequestException("431 Request Header Fields Too Large");

                                headers[headerKey!] = value;
                                headerKey = null;
                                state = ParseState.HeaderLf;
                            }
                            else
                            {
                                currentToken.Append(ch);
                            }

                            break;

                        case ParseState.HeaderLf:
                            if (ch == '\n')
                            {
                                state = ParseState.HeaderKey;
                            }
                            else
                            {
                                throw new HttpRequestException("Ожидался LF после CR");
                            }

                            break;

                        case ParseState.Body:
                            if (headers.TryGetValue("Content-Length", out var cl))
                            {
                                if (!int.TryParse(cl, out contentLength) || contentLength < 0)
                                    throw new HttpRequestException("Некорректный Content-Length");

                                if (contentLength > options.MaxRequestSize)
                                    throw new HttpRequestException("413 Payload Too Large");

                                int remaining = contentLength - totalBodySize;
                                int toRead = Math.Min(remaining, count - offset);
                                if (toRead > 0)
                                {
                                    await bodyBuffer.WriteAsync(buffer, offset, toRead);
                                    offset += toRead;
                                    totalBodySize += toRead;
                                }

                                if (totalBodySize >= contentLength)
                                    state = ParseState.Done;
                            }
                            else if (headers.TryGetValue("Transfer-Encoding", out var te) &&
                                     te.Contains("chunked", StringComparison.OrdinalIgnoreCase))
                            {
                                state = ParseState.ChunkSize;
                            }
                            else
                            {
                                state = ParseState.Done;
                            }

                            break;

                        case ParseState.ChunkSize:
                            if (ch == '\r')
                            {
                                // пропускаем
                            }
                            else if (ch == '\n')
                            {
                                if (!int.TryParse(currentToken.ToString(), System.Globalization.NumberStyles.HexNumber,
                                        null, out chunkSize))
                                    throw new HttpRequestException("Некорректный размер чанка");

                                if (chunkSize == 0)
                                {
                                    state = ParseState.Trailer;
                                }
                                else
                                {
                                    if (totalBodySize + chunkSize > options.MaxRequestSize)
                                        throw new HttpRequestException("413 Payload Too Large");
                                    chunkBytesRead = 0;
                                    state = ParseState.ChunkData;
                                }

                                currentToken.Clear();
                            }
                            else
                            {
                                currentToken.Append(ch);
                            }

                            break;

                        case ParseState.ChunkData:
                            int remainingChunk = chunkSize - chunkBytesRead;
                            int toReadChunk = Math.Min(remainingChunk, count - offset);
                            if (toReadChunk > 0)
                            {
                                await bodyBuffer.WriteAsync(buffer, offset, toReadChunk);
                                offset += toReadChunk;
                                chunkBytesRead += toReadChunk;
                                totalBodySize += toReadChunk;
                            }

                            if (chunkBytesRead >= chunkSize)
                            {
                                state = ParseState.ChunkCr;
                            }

                            break;

                        case ParseState.ChunkCr:
                            if (ch == '\r')
                            {
                                // ok
                            }
                            else if (ch == '\n')
                            {
                                state = ParseState.ChunkSize;
                            }
                            else
                            {
                                throw new HttpRequestException("Ожидался CRLF после данных чанка");
                            }

                            break;

                        case ParseState.Trailer:
                            if (ch == '\r')
                            {
                                // конец трейлера
                            }
                            else if (ch == '\n')
                            {
                                if (currentToken.Length == 0)
                                    state = ParseState.Done;
                                else
                                    currentToken.Clear();
                            }
                            else
                            {
                                currentToken.Append(ch);
                            }

                            break;
                    }
                }
            }

            // Декодируем тело, если оно есть
            if (bodyBuffer.Length > 0)
            {
                try
                {
                    request.Body = Encoding.UTF8.GetString(bodyBuffer.ToArray());
                }
                catch (DecoderFallbackException ex)
                {
                    throw new HttpRequestException("Некорректная UTF-8 последовательность в теле запроса", ex);
                }
            }
            else
            {
                request.Body = "";
            }

            request.Headers = headers;
            return request;
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            // Таймаут ожидания данных
            throw new HttpRequestException("408 Request Timeout", true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Сервер останавливается
            return null;
        }
    }

    public static async Task SendResponseAsync(
        NetworkStream stream,
        HttpResponse response,
        CancellationToken cancellationToken,
        bool keepAlive = false)
    {
        var sb = new StringBuilder();
        sb.Append($"HTTP/1.1 {response.StatusCode} {response.StatusMessage}\r\n");

        // Пользовательские заголовки
        foreach (var header in response.Headers)
        {
            sb.Append($"{header.Key}: {header.Value}\r\n");
        }

        // Стандартные заголовки, если не заданы
        if (!response.Headers.ContainsKey("Content-Type"))
            sb.Append($"Content-Type: {response.ContentType}\r\n");

        if (!response.Headers.ContainsKey("Content-Length"))
        {
            long contentLength = response.BodyStream?.Length ?? Encoding.UTF8.GetByteCount(response.Content);
            sb.Append($"Content-Length: {contentLength}\r\n");
        }

        sb.Append($"Connection: {(keepAlive ? "keep-alive" : "close")}\r\n");
        sb.Append("\r\n");

        await stream.WriteAsync(Encoding.UTF8.GetBytes(sb.ToString()).AsMemory(0, sb.Length), cancellationToken);

        if (response.BodyStream != null)
        {
            await response.BodyStream.CopyToAsync(stream, 81920, cancellationToken);
        }
        else if (!string.IsNullOrEmpty(response.Content))
        {
            await stream.WriteAsync(Encoding.UTF8.GetBytes(response.Content).AsMemory(0, response.Content.Length),
                cancellationToken);
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
}