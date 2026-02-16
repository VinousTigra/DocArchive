#nullable enable

using System.Collections.Generic;

namespace DocArhive;

public class HttpRequest
{
    public string Method { get; set; } = "";
    public string Path { get; set; } = "";
    public Dictionary<string, string> Headers { get; set; } = new();
    public string Body { get; set; } = "";
}