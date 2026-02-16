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

    /// <summary>
    /// Читает HTTP-запрос из потока с использованием конечного автомата и буферизованного чтения.
    /// </summary>
    public static async Task<HttpRequest?> ReadHttpRequestAsync(
        Stream stream,
        HttpServerOptions options,
        CancellationToken cancellationToken)
    {
        // Таймаут на чтение всего запроса
        using var timeoutCts = new CancellationTokenSource();
        timeoutCts.CancelAfter(options.ReceiveTimeoutMs);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        var combinedToken = linkedCts.Token;

        var buffer = new byte[8192]; // 8 КБ буфер
        var request = new HttpRequest();
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var state = ParseState.Method;
        var currentToken = new StringBuilder(128);
        string? headerKey = null;
        int contentLength = -1;
        int chunkSize = 0;
        int bodyRead = 0;
        var bodyBuffer = new MemoryStream();
        

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
                        if (state != ParseState.Done && state != ParseState.Body)
                            throw new HttpRequestException("Неожиданный конец потока");
                        break;
                    }
                    offset = 0;
                }

                while (offset < count && state != ParseState.Done)
                {
                    byte b = buffer[offset++];
                    char ch = (char)b;

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
                                // пустая строка – конец заголовков
                                currentToken.Clear();
                                state = ParseState.Body;
                            }
                            else if (ch == '\n')
                            {
                                // игнорируем возможный лишний \n
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

                                int remaining = contentLength - bodyRead;
                                int toRead = Math.Min(remaining, count - offset);
                                if (toRead > 0)
                                {
                                    await bodyBuffer.WriteAsync(buffer, offset, toRead);
                                    offset += toRead;
                                    bodyRead += toRead;
                                }
                                if (bodyRead >= contentLength)
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
                                if (!int.TryParse(currentToken.ToString(), System.Globalization.NumberStyles.HexNumber, null, out chunkSize))
                                    throw new HttpRequestException("Некорректный размер чанка");

                                if (chunkSize == 0)
                                {
                                    state = ParseState.Trailer;
                                }
                                else
                                {
                                    if (bodyBuffer.Length + chunkSize > options.MaxRequestSize)
                                        throw new HttpRequestException("413 Payload Too Large");
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
                            int remainingChunk = chunkSize - (int)(bodyBuffer.Length - bodyRead);
                            int toReadChunk = Math.Min(remainingChunk, count - offset);
                            if (toReadChunk > 0)
                            {
                                await bodyBuffer.WriteAsync(buffer, offset, toReadChunk);
                                offset += toReadChunk;
                                bodyRead += toReadChunk;
                            }
                            if (bodyRead - (int)bodyBuffer.Length == 0) // все данные чанка записаны?
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

            request.Headers = headers;
            request.Body = bodyBuffer.Length > 0 ? Encoding.UTF8.GetString(bodyBuffer.ToArray()) : "";

            return request;
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            // Таймаут чтения запроса
            throw new HttpRequestException("408 Request Timeout", true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Сервер останавливается – просто выходим
            return null;
        }
        catch (TimeoutException)
        {
            throw new HttpRequestException("408 Request Timeout", true);
        }
    }

    /// <summary>
    /// Отправляет HTTP-ответ в поток.
    /// </summary>
    public static async Task SendResponseAsync(
        Stream stream,
        HttpResponse response,
        CancellationToken cancellationToken,
        bool keepAlive = false)
    {
        var header = $"HTTP/1.1 {response.StatusCode} {response.StatusMessage}\r\n" +
                     $"Content-Type: {response.ContentType}\r\n" +
                     $"Content-Length: {Encoding.UTF8.GetByteCount(response.Content)}\r\n" +
                     $"Connection: {(keepAlive ? "keep-alive" : "close")}\r\n" +
                     "\r\n";

        var headerBytes = Encoding.UTF8.GetBytes(header);
        await stream.WriteAsync(headerBytes.AsMemory(0, headerBytes.Length), cancellationToken);

        if (response.BodyStream != null)
        {   
            await response.BodyStream.CopyToAsync(stream, 81920, cancellationToken);
        }
        else if (!string.IsNullOrEmpty(response.Content))
        {
            var bodyBytes = Encoding.UTF8.GetBytes(response.Content);
            await stream.WriteAsync(bodyBytes.AsMemory(0, bodyBytes.Length), cancellationToken);
        }

        // Принудительно отправляем данные (на всякий случай)
        await stream.FlushAsync(cancellationToken);
    }
}

/// <summary>
/// Исключение, возникающее при ошибках парсинга HTTP.
/// </summary>
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