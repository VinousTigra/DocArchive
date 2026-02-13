namespace DocArhive.Controllers;

using System.Text;
using Models;

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
            for (int i = 0; i < 3; i++)
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

        Console.WriteLine($"Поиск файла: {filePath}");

        if (File.Exists(filePath))
        {
            Console.WriteLine($"Файл найден: {fileName}");
            return File.ReadAllText(filePath, Encoding.UTF8);
        }

        Console.WriteLine($"Файл НЕ найден: {fileName}");

        // Fallback - простой HTML
        return fileName;
    }

    public string Index()
    {
        return ReadViewFile("index.html");
    }

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
                documentsHtml.AppendLine($"        <td>{doc.Title}</td>");
                documentsHtml.AppendLine($"        <td>{doc.Category}</td>");
                documentsHtml.AppendLine($"        <td>{doc.UploadDate:yyyy-MM-dd HH:mm}</td>");
                documentsHtml.AppendLine($"        <td>{doc.FileName}</td>");
                documentsHtml.AppendLine("    </tr>");
            }

            documentsHtml.AppendLine("</tbody>");
            documentsHtml.AppendLine("</table>");
        }
        else
        {
            documentsHtml.AppendLine("<div style='padding: 20px; background: #f8f9fa; text-align: center;'>");
            documentsHtml.AppendLine("    <p>Не загружено ни одного документа</p>");
            documentsHtml.AppendLine("    <p><a href='/'>Добавить документ</a></p>");
            documentsHtml.AppendLine("</div>");
        }

        var html = template
            .Replace("{{documents_list}}", documentsHtml.ToString());

        return html;
    }
}