#nullable enable

using System;
using System.Collections.Generic;
using System.IO;

namespace DocArhive;

public class HttpResponse
{
    public int StatusCode { get; private set; }
    public string StatusMessage { get; private set; }
    public string ContentType { get; set; } = "text/html; charset=utf-8";
    public string Content { get; set; } = "";
    public Stream? BodyStream { get; set; }
    public bool? KeepAlive { get; set; } = null;
    public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private HttpResponse(int statusCode, string statusMessage)
    {
        StatusCode = statusCode;
        StatusMessage = statusMessage;
    }

    public static HttpResponse Ok(string content = "", string contentType = "text/html; charset=utf-8")
        => new(200, "OK") { Content = content, ContentType = contentType };

    public static HttpResponse Redirect(string location)
    {
        var response = new HttpResponse(302, "Found");
        response.Headers["Location"] = location;
        return response;
    }

    public static HttpResponse BadRequest(string message = "Bad Request")
        => new(400, "Bad Request") { Content = $"<h1>400 Bad Request</h1><p>{message}</p>" };

    public static HttpResponse NotFound(string message = "Not Found")
        => new(404, "Not Found") { Content = $"<h1>404 Not Found</h1><p>{message}</p>" };

    public static HttpResponse Timeout(string message = "Request Timeout")
        => new(408, "Request Timeout") { Content = $"<h1>408 Request Timeout</h1><p>{message}</p>" };

    public static HttpResponse PayloadTooLarge(string message = "Payload Too Large")
        => new(413, "Payload Too Large") { Content = $"<h1>413 Payload Too Large</h1><p>{message}</p>" };

    public static HttpResponse RequestHeaderFieldsTooLarge(string message = "Request Header Fields Too Large")
        => new(431, "Request Header Fields Too Large") { Content = $"<h1>431 Request Header Fields Too Large</h1><p>{message}</p>" };

    public static HttpResponse InternalServerError(string message = "Internal Server Error")
        => new(500, "Internal Server Error") { Content = $"<h1>500 Internal Server Error</h1><p>{message}</p>" };

    public static HttpResponse MethodNotAllowed()
        => new(405, "Method Not Allowed") { Content = "<h1>405 Method Not Allowed</h1>" };

    public static HttpResponse NotImplemented()
        => new(501, "Not Implemented") { Content = "<h1>501 Not Implemented</h1>" };

    public static HttpResponse Empty(int statusCode = 204, string statusMessage = "No Content")
        => new(statusCode, statusMessage) { Content = "" };
}