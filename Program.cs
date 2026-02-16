using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DocArhive;

class Program
{
    static async Task Main()
    {
        var options = new HttpServerOptions
        {
            Port = 8080,
            IpAddress = "127.0.0.1",
            MaxConcurrentConnections = 50,
            MaxRequestSize = 40 * 1024 * 1024,
            MaxHeaderSize = 64 * 1024,
            MaxHeaderCount = 64,
            ReceiveTimeoutMs = 10000,
            SendTimeoutMs = 5000,
            KeepAliveTimeoutMs = 15000
        };

        var services = new ServiceCollection();
        services.AddLogging(builder => builder
            .AddConsole()
            .SetMinimumLevel(LogLevel.Information)
        );
        services.AddSingleton(options);
        services.AddSingleton<HttpServer>();

        var serviceProvider = services.BuildServiceProvider();
        var server = serviceProvider.GetRequiredService<HttpServer>();

        var tcs = new TaskCompletionSource();

        Console.CancelKeyPress += async (sender, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("Получен сигнал остановки. Завершаем работу...");
            await server.StopAsync();
            tcs.SetResult();
        };

        try
        {
            server.Start();
            Console.WriteLine("Сервер запущен. Нажмите Ctrl+C для остановки...");
            await tcs.Task;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Критическая ошибка: {ex.Message}");
        }
    }
}