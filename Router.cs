using System;
using System.Collections.Generic;

namespace DocArhive;

using System.Net;
using Controllers;
using Models;

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
                _ => new HttpResponse
                {
                    StatusCode = 405,
                    StatusMessage = "Method Not Allowed",
                    Content = "<h1>405 Method Not Allowed</h1>"
                }
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка маршрутизации: {ex.Message}");
            return new HttpResponse
            {
                StatusCode = 500,
                StatusMessage = "Internal Server Error",
                Content = $"<h1>500 Internal Server Error</h1><p>{ex.Message}</p>"
            };
        }
    }
    
    private HttpResponse RouteGet(HttpRequest request)
    {
        return request.Path switch
        {
            "/" => Ok(_homeController.Index()),
            "/status" => Ok(_homeController.Status()),
            _ => NotFound()
        };
    }
    
    private HttpResponse RoutePost(HttpRequest request)
    {
        if (request.Path == "/action")
        {
            var formData = ParseFormData(request.Body);
            var result = _archiveController.AddDocument(formData);
            return Ok(result);
        }
        
        return NotFound();
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
    
    private static HttpResponse Ok(string content) => new()
    {
        StatusCode = 200,
        StatusMessage = "OK",
        Content = content
    };
    
    private static HttpResponse NotFound() => new()
    {
        StatusCode = 404,
        StatusMessage = "Not Found",
        Content = "<h1>404 Not Found</h1><p>Страница не найдена</p>"
    };
}