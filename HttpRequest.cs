#nullable enable

using System.Collections.Generic;

namespace DocArhive;

public class HttpRequest
{
    public string Method { get; init; } = "";
    public string Path { get; init; } = "";
    public Dictionary<string, string> Headers { get; init; } = new();
    public string Body { get; init; } = "";
}