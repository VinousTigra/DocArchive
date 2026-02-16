#nullable enable

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
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

    public Task<HttpResponse> RouteAsync(HttpRequest request)
    {
        try
        {
            return request.Method.ToUpperInvariant() switch
            {
                "GET" => RouteGetAsync(request),
                "POST" => RoutePostAsync(request),
                _ => Task.FromResult(HttpResponse.MethodNotAllowed())
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка маршрутизации: {ex.Message}");
            return Task.FromResult(HttpResponse.InternalServerError("Внутренняя ошибка сервера"));
        }
    }

    private Task<HttpResponse> RouteGetAsync(HttpRequest request)
    {
        return request.Path switch
        {
            "/" => Task.FromResult(HttpResponse.Ok(_homeController.Index())),
            "/status" => Task.FromResult(HttpResponse.Ok(_homeController.Status())),
            "/health" => Task.FromResult(HttpResponse.Ok("Healthy", "text/plain")),
            _ => Task.FromResult(HttpResponse.NotFound("Страница не найдена"))
        };
    }

    private Task<HttpResponse> RoutePostAsync(HttpRequest request)
    {
        if (request.Path == "/action")
        {
            var formData = ParseFormData(request.Body);
            var result = _archiveController.AddDocument(formData);
            return Task.FromResult(HttpResponse.Ok(result));
        }
        return Task.FromResult(HttpResponse.NotFound());
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