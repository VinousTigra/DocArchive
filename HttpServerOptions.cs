namespace DocArhive;

public class HttpServerOptions
{
    public int Port { get; set; } = 8080;
    public string IpAddress { get; set; } = "127.0.0.1";
    public int MaxConcurrentConnections { get; set; } = 100;
    public int MaxRequestSize { get; set; } = 10 * 1024 * 1024;      // 1 MB
    public int MaxHeaderSize { get; set; } = 32 * 1024;         // 16 KB (приблизительно)
    public int MaxHeaderCount { get; set; } = 32;
    public int ReceiveTimeoutMs { get; set; } = 5000;           // используется для таймаута чтения строк
    public int SendTimeoutMs { get; set; } = 5000;
    public int KeepAliveTimeoutMs { get; set; } = 30000;  // минута ожидания следующего запроса
}