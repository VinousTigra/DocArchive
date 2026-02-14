#nullable enable

namespace DocArhive;

public class HttpResponse
{
    public int StatusCode { get; private set; }
    public string StatusMessage { get; private set; }
    public string ContentType { get; set; } = "text/html; charset=utf-8";
    public string Content { get; set; } = "";

    private HttpResponse(int statusCode, string statusMessage)
    {
        StatusCode = statusCode;
        StatusMessage = statusMessage;
    }

    public static HttpResponse Ok(string content = "", string contentType = "text/html; charset=utf-8")
        => new(200, "OK") { Content = content, ContentType = contentType };

    public static HttpResponse BadRequest(string message = "Bad Request")
        => new(400, "Bad Request") { Content = $"<h1>400 Bad Request</h1><p>{message}</p>" };

    public static HttpResponse NotFound(string message = "Not Found")
        => new(404, "Not Found") { Content = $"<h1>404 Not Found</h1><p>{message}</p>" };

    public static HttpResponse Timeout(string message = "Request Timeout")
        => new(408, "Request Timeout") { Content = $"<h1>408 Request Timeout</h1><p>{message}</p>" };

    public static HttpResponse InternalServerError(string message = "Internal Server Error")
        => new(500, "Internal Server Error") { Content = $"<h1>500 Internal Server Error</h1><p>{message}</p>" };

    public static HttpResponse MethodNotAllowed()
        => new(405, "Method Not Allowed") { Content = "<h1>405 Method Not Allowed</h1>" };

    public static HttpResponse NotImplemented()
        => new(501, "Not Implemented") { Content = "<h1>501 Not Implemented</h1>" };
}