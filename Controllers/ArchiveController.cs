#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using DocArhive.Models;
using Microsoft.Extensions.Logging;

namespace DocArhive.Controllers;

public class ArchiveController
{
    private readonly ProjectState _state;
    private readonly ILogger<ArchiveController> _logger;

    public ArchiveController(ProjectState state, ILogger<ArchiveController> logger)
    {
        _state = state;
        _logger = logger;
    }

    public void AddDocument(Dictionary<string, List<string>> formData)
    {
        string? title = GetLastValue(formData, "title");
        string? description = GetLastValue(formData, "description");
        string? filename = GetLastValue(formData, "filename");
        string? category = GetLastValue(formData, "category");

        _logger.LogInformation("Добавление документа: title={Title}, filename={Filename}", title, filename);

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(filename))
        {
            throw new ArgumentException("Название документа и имя файла обязательны для заполнения");
        }

        var document = new Document(
            title,
            description ?? "",
            filename,
            category ?? "Без категории"
        );

        _state.AddDocument(document);
        _logger.LogInformation("Документ добавлен с ID {Id}", document.Id);
    }

    private static string? GetLastValue(Dictionary<string, List<string>> dict, string key)
    {
        return dict.TryGetValue(key, out var list) && list.Count > 0 ? list.Last() : null;
    }
}