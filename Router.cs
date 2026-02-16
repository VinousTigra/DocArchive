#nullable enable

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using DocArhive.Controllers;
using Microsoft.Extensions.Logging;

namespace DocArhive;

public class Router
{
    private readonly HomeController _homeController;
    private readonly ArchiveController _archiveController;
    private readonly ILogger<Router> _logger;

    public Router(HomeController homeController, ArchiveController archiveController, ILogger<Router> logger)
    {
        _homeController = homeController;
        _archiveController = archiveController;
        _logger = logger;
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
            _logger.LogError(ex, "Ошибка маршрутизации");
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
            "/favicon.ico" => Task.FromResult(HttpResponse.Empty(204)), // No Content
            _ => Task.FromResult(HttpResponse.NotFound("Страница не найдена"))
        };
    }

    private Task<HttpResponse> RoutePostAsync(HttpRequest request)
    {
        if (request.Path == "/action")
        {
            try
            {
                var formData = ParseFormData(request.Body);
                _logger.LogInformation("Получены поля формы: {Fields}", string.Join(", ", formData.Keys));
                _archiveController.AddDocument(formData);
                return Task.FromResult(HttpResponse.Redirect("/status"));
            }
            catch (ArgumentException ex) // ошибка валидации
            {
                _logger.LogWarning("Ошибка валидации: {Message}", ex.Message);
                return Task.FromResult(HttpResponse.BadRequest(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обработки POST /action");
                return Task.FromResult(HttpResponse.InternalServerError("Внутренняя ошибка сервера"));
            }
        }
        return Task.FromResult(HttpResponse.NotFound());
    }

    private static Dictionary<string, List<string>> ParseFormData(string body)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(body))
            return result;

        var pairs = body.Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var keyValue = pair.Split('=');
            if (keyValue.Length != 2) continue;

            var key = WebUtility.UrlDecode(keyValue[0]);
            var value = WebUtility.UrlDecode(keyValue[1]);

            if (key.EndsWith("[]", StringComparison.Ordinal))
                key = key[..^2];

            if (!result.ContainsKey(key))
                result[key] = new List<string>();

            result[key].Add(value);
        }
        return result;
    }
}