namespace DocArhive;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Запуск...");

        var options = new HttpServerOptions
        {
            Port = 8080,
            IpAddress = "127.0.0.1",
            MaxConcurrentConnections = 50,
            MaxRequestSize = 1024 * 1024,      // 1 MB
            MaxHeaderSize = 16 * 1024,         // 16 KB
            MaxHeaderCount = 32,
            ReceiveTimeoutMs = 10000,          // 10 секунд
            SendTimeoutMs = 5000              // 5 секунд
        };

        using var server = new HttpServer(options);

        try
        {
            server.Start();
            Console.WriteLine("Нажмите Enter для остановки сервера...");
            Console.ReadLine();
            await server.StopAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Критическая ошибка: {ex.Message}");
        }
    }
}