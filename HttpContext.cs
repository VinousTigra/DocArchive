#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DocArhive;

public abstract class HttpContext(HttpRequest request)
{
    public HttpRequest Request { get; } = request;
    public HttpResponse Response { get; set; } = null!;
    public CancellationToken RequestAborted { get; set; }
    public IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
}

public delegate Task RequestDelegate(HttpContext context);