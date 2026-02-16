using System;
using System.Threading;
using System.Threading.Tasks;
using DocArhive.Controllers;
using DocArhive.Models;
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
            MaxRequestSize = 30*1024 * 1024,
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
        services.AddSingleton<ProjectState>();
        services.AddSingleton<HomeController>();
        services.AddSingleton<ArchiveController>();
        services.AddSingleton<Router>();
        services.AddSingleton<HttpServer>();

        var serviceProvider = services.BuildServiceProvider();
        HttpServer server = serviceProvider.GetRequiredService<HttpServer>();

        var cts = new CancellationTokenSource();

        Console.CancelKeyPress += async (sender, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("Получен сигнал остановки. Завершаем работу...");
            await server.StopAsync();
            cts.Cancel();
        };

        try
        {
            server.Start();
            Console.WriteLine("Нажмите Ctrl+C для остановки сервера...");
            await Task.Delay(-1, cts.Token);
        }
        catch (TaskCanceledException)
        {
            // нормальное завершение
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Критическая ошибка: {ex.Message}");
        }
    }
}