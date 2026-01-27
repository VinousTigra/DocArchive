using DocArhive;

Console.WriteLine("Запуск веб-сервера электронного архива...");

var server = new HttpServer("127.0.0.1", 8080);

try
{
    server.Start();
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка при запуске сервера: {ex.Message}");
}