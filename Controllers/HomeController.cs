using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using DocArhive.Models;

namespace DocArhive.Controllers;

public class HomeController
{
    private readonly ProjectState _state;
    private readonly string _viewsPath;

    public HomeController(ProjectState state)
    {
        _state = state;
        var projectRoot = GetProjectRoot();
        _viewsPath = Path.Combine(projectRoot, "Views");
    }

    private string GetProjectRoot()
    {
        // Получаем текущую директорию (где запущена программа)
        var currentDir = Directory.GetCurrentDirectory();

        // Если мы в bin/Debug или bin/Release, поднимаемся на уровень проекта
        var binDebugPattern = Path.Combine("bin", "Debug");
        var binReleasePattern = Path.Combine("bin", "Release");

        if (currentDir.Contains(binDebugPattern) || currentDir.Contains(binReleasePattern))
        {
            // Поднимаемся на 3 уровня вверх из bin/Debug/netX.0/
            var projectRoot = currentDir;
            for (var i = 0; i < 3; i++)
            {
                projectRoot = Directory.GetParent(projectRoot)?.FullName;
                if (projectRoot == null) break;
            }

            return projectRoot ?? currentDir;
        }

        return currentDir;
    }

    private string ReadViewFile(string fileName)
    {
        var filePath = Path.Combine(_viewsPath, fileName);
        if (File.Exists(filePath))
        {
            Console.WriteLine($"Файл найден: {filePath}");
            return File.ReadAllText(filePath, Encoding.UTF8);
        }

        Console.WriteLine($"Файл не найден: {filePath}, использую встроенный шаблон");
        return fileName switch
        {
            "index.html" => GetFallbackIndexHtml(),
            "status.html" => GetFallbackStatusHtml(),
            _ => "<html><body><h1>Страница не найдена</h1></body></html>"
        };
    }

    public string Index()
    {
        //return ReadViewFile("index.html");
        var template = ReadViewFile("index.html");
        var documentsHtml = new StringBuilder();
        documentsHtml.AppendLine("<div>");
        documentsHtml.AppendLine($"<p> Всего документов в архиве: {_state.Documents.Count}</p>");
        documentsHtml.AppendLine("</div>");

        return template.Replace("{{documentsCount}}", documentsHtml.ToString());
    }

    // HomeController.cs — исправленный метод Status
    public string Status()
    {
        var template = ReadViewFile("status.html");

        var documentsHtml = new StringBuilder();

        if (_state.Documents.Any())
        {
            documentsHtml.AppendLine("<table>");
            documentsHtml.AppendLine("<thead>");
            documentsHtml.AppendLine("    <tr>");
            documentsHtml.AppendLine("        <th>ID</th>");
            documentsHtml.AppendLine("        <th>Название</th>");
            documentsHtml.AppendLine("        <th>Категория</th>");
            documentsHtml.AppendLine("        <th>Дата загрузки</th>");
            documentsHtml.AppendLine("        <th>Имя файла</th>");
            documentsHtml.AppendLine("    </tr>");
            documentsHtml.AppendLine("</thead>");
            documentsHtml.AppendLine("<tbody>");

            foreach (var doc in _state.Documents)
            {
                documentsHtml.AppendLine("    <tr>");
                documentsHtml.AppendLine($"        <td>{doc.Id}</td>");
                documentsHtml.AppendLine($"        <td>{WebUtility.HtmlEncode(doc.Title)}</td>");
                documentsHtml.AppendLine($"        <td>{WebUtility.HtmlEncode(doc.Category)}</td>");
                documentsHtml.AppendLine($"        <td>{doc.UploadDate:yyyy-MM-dd HH:mm}</td>");
                documentsHtml.AppendLine($"        <td>{WebUtility.HtmlEncode(doc.FileName)}</td>");
                documentsHtml.AppendLine("    </tr>");
            }

            documentsHtml.AppendLine("</tbody>");
            documentsHtml.AppendLine("</table>");
            documentsHtml.AppendLine("<div>");
            documentsHtml.AppendLine($"<p> Всего документов в архиве: {_state.Documents.Count}</p>");
            documentsHtml.AppendLine("</div>");
        }
        else
        {
            documentsHtml.AppendLine("<div style='padding: 20px; background: #f8f9fa; text-align: center;'>");
            documentsHtml.AppendLine("    <p>Не загружено ни одного документа</p>");
            documentsHtml.AppendLine("    <p><a href='/'>Добавить документ</a></p>");
            documentsHtml.AppendLine("</div>");
        }

        var html = template
            .Replace("{{total_documents}}", _state.TotalDocuments.ToString())
            .Replace("{{documents_list}}", documentsHtml.ToString());

        return html;
    }


    private string GetFallbackIndexHtml()
    {
        return @"<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Электронный архив</title>
    <style>
        body { font-family: Arial; margin: 40px; background: #f5f5f5; }
        .container { max-width: 800px; margin: 0 auto; background: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        h1 { color: #2c3e50; border-bottom: 3px solid #3498db; padding-bottom: 10px; }
        .nav { margin: 20px 0; padding: 15px; background: #3498db; border-radius: 5px; }
        .nav a { color: white; text-decoration: none; padding: 10px 15px; background: #2980b9; border-radius: 4px; margin-right: 10px; }
        .nav a:hover { background: #1c5a7a; }
        form { margin: 20px 0; }
        input, textarea { width: 100%; padding: 8px; margin: 5px 0; border: 1px solid #ddd; border-radius: 4px; }
        button { background: #28a745; color: white; padding: 12px; border: none; border-radius: 4px; cursor: pointer; width: 100%; transition: background 0.3s; }
        button:hover { background: #d02f66; }
        .stats { display: none; } /* скрываем блок статистики, если он есть */
    </style>
</head>
<body>
    <div class='container'>
        <h1>📁 Электронный архив документов</h1>
        <div class='nav'>
            <a href='/'>🏠 Главная</a>
            <a href='/status'>📊 Статус архива</a>
        </div>
        <h2>➕ Добавить новый документ</h2>
        <form method='post' action='/action'>
            <input type='text' name='title' placeholder='Название' required>
            <textarea name='description' placeholder='Описание' rows='3'></textarea>
            <input type='text' name='filename' placeholder='Имя файла' required>
            <input type='text' name='category' placeholder='Категория'>
            <button type='submit'>📤 Добавить документ в архив</button>
        </form>
        <div class='stats'>
            {{documentsCount}}
        </div>
    </div>
</body>
</html>";
    }

    private string GetFallbackStatusHtml()
    {
        return @"<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Статус архива</title>
    <style>
        body { font-family: Arial; margin: 40px; background: #f5f5f5; }
        .container { max-width: 1200px; margin: 0 auto; background: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        h1 { color: #2c3e50; border-bottom: 3px solid #3498db; padding-bottom: 10px; }
        .nav { margin: 20px 0; padding: 15px; background: #3498db; border-radius: 5px; }
        .nav a { color: white; text-decoration: none; padding: 10px 15px; background: #2980b9; border-radius: 4px; }
        .stats { background: #2c3e50; color: white; padding: 20px; border-radius: 5px; margin: 20px 0; }
        table { width: 100%; border-collapse: collapse; margin: 20px 0; }
        th, td { border: 1px solid #ddd; padding: 12px; text-align: left; }
        th { background: #3498db; color: white; }
        tr:nth-child(even) { background: #f9f9f9; }
    </style>
</head>
<body>
    <div class='container'>
        <h1>📊 Состояние электронного архива</h1>
        <div class='nav'>
            <a href='/'>🏠 Главная</a>
            <a href='/status'>🔄 Обновить</a>
        </div>
        <div class='stats'>
            <h2>Общая статистика</h2>
            <p><strong>Всего документов:</strong> {{total_documents}}</p>
            <p><strong>Последнее обновление:</strong> {{last_update}}</p>
        </div>
        <h2>📁 Список документов</h2>
        {{documents_list}}
    </div>
</body>
</html>";
    }
}