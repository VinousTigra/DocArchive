using System.Net;
using DocArhive.Controllers;
using DocArhive.Models;

namespace DocArhive;

public class Router
{
    private readonly HomeController _homeController;
    private readonly ArchiveController _archiveController;

    public Router(ProjectState projectState)
    {
        _homeController = new HomeController(projectState);
        _archiveController = new ArchiveController(projectState);
    }

    public HttpResponse Route(HttpRequest request)
    {
        try
        {
            return request.Method.ToUpperInvariant() switch
            {
                "GET" => RouteGet(request),
                "POST" => RoutePost(request),
                _ => HttpResponse.MethodNotAllowed()
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка маршрутизации: {ex.Message}");
            return HttpResponse.InternalServerError("Внутренняя ошибка сервера");
        }
    }

    private HttpResponse RouteGet(HttpRequest request)
    {
        return request.Path switch
        {
            "/" => HttpResponse.Ok(_homeController.Index()),
            "/status" => HttpResponse.Ok(_homeController.Status()),
            _ => HttpResponse.NotFound("Страница не найдена")
        };
    }

    private HttpResponse RoutePost(HttpRequest request)
    {
        if (request.Path == "/action")
        {
            var formData = ParseFormData(request.Body);
            var result = _archiveController.AddDocument(formData);
            return HttpResponse.Ok(result);
        }

        return HttpResponse.NotFound();
    }

    private static Dictionary<string, string> ParseFormData(string body)
    {
        var formData = new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(body))
        {
            var pairs = body.Split('&');
            foreach (var pair in pairs)
            {
                var keyValue = pair.Split('=');
                if (keyValue.Length == 2)
                {
                    var key = WebUtility.UrlDecode(keyValue[0]);
                    var value = WebUtility.UrlDecode(keyValue[1]);
                    formData[key] = value;
                }
            }
        }

        return formData;
    }
}