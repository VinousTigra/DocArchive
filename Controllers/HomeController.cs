namespace DocArhive.Controllers;

using System.Text;
using Models;

public class HomeController
{
    private readonly ProjectState _state;
    private readonly string _basePath;
    
    public HomeController(ProjectState state)
    {
        _state = state;
        
        // Определяем базовый путь
        var currentDir = Directory.GetCurrentDirectory();
        _basePath = currentDir;
        
        // Если мы в bin/Debug/net9.0, поднимаемся на уровень проекта
        if (currentDir.Contains("bin\\Debug") || currentDir.Contains("bin/Debug"))
        {
            _basePath = Path.Combine(currentDir, "..", "..", "..", "..");
        }
        
        Console.WriteLine($"Base path: {_basePath}");
    }
    
    private string ReadViewFile(string fileName)
    {
        var filePath = Path.Combine(_basePath, "Views", fileName);
        Console.WriteLine($"Looking for file: {filePath}");
        
        if (File.Exists(filePath))
        {
            return File.ReadAllText(filePath);
        }
        
        // Если файл не найден в корне проекта, ищем в текущей директории
        filePath = Path.Combine(Directory.GetCurrentDirectory(), "Views", fileName);
        Console.WriteLine($"File not found, trying: {filePath}");
        
        if (File.Exists(filePath))
        {
            return File.ReadAllText(filePath);
        }
        
        // Fallback - простой HTML
        return fileName switch
        {
            "index.html" => GetSimpleIndexHtml(),
            "status.html" => GetSimpleStatusHtml(),
            _ => "<html><body><h1>Page not found</h1></body></html>"
        };
    }
    
    private string GetSimpleIndexHtml()
    {
        return """
            <!DOCTYPE html>
            <html>
            <head>
                <title>Электронный архив</title>
                <style>
                    body { font-family: cursive; margin: 40px; }
                    a { display: inline-block; padding: 10px 20px; background: #007bff; color: white; text-decoration: none; margin: 5px; border-radius: 4px; }
                    a:hover { background: #0056b3; }
                    form { margin: 20px 0; padding: 20px; background: #f8f9fa; border-radius: 5px; }
                    input, textarea { display: block; margin: 10px 0; padding: 8px; width: 300px; }
                    button { padding: 10px 20px; background: #28a745; color: white; border: none; border-radius: 4px; }
                </style>
            </head>
            <body>
                <h1>Архив документов</h1>
                <a href="/">Главная</a>
                <a href="/status">Состояние архива</a>
                <h2>Добавить новый документ</h2>
                <form method="post" action="/action">
                    <input type="text" name="title" placeholder="Название" required>
                    <textarea name="description" placeholder="Описание" rows="3"></textarea>
                    <input type="text" name="filename" placeholder="Имя файла" required>
                    <input type="text" name="category" placeholder="Категория">
                    <button type="submit">Добавить в архив</button>
                </form>
            </body>
            </html>
            """;
    }
    
    private string GetSimpleStatusHtml()
    {
        return """
            <!DOCTYPE html>
            <html>
            <head>
                <title>Статус архива</title>
                <style>
                    body { font-family: cursive; margin: 40px; }
                    a { display: inline-block; padding: 10px 20px; background: #007bff; color: white; text-decoration: none; margin: 5px; border-radius: 4px; }
                    a:hover { background: #0056b3; }
                    table { border-collapse: collapse; width: 100%; margin: 20px 0; }
                    th, td { border: 1px solid #ddd; padding: 12px; text-align: left; }
                    th { background-color: #007bff; color: white; }
                    .stats { background: #d02f66; padding: 20px; border-radius: 5px; margin: 20px 0; }
                </style>
            </head>
            <body>
                <h1>Состояние электронного архива</h1>
                <a href="/">Главная</a>
                <a href="/status">Обновить статус</a>
                <div class="stats">
                    <h2>Статистика архива</h2>
                    <p><strong>Всего документов:</strong> {{total_documents}}</p>
                    <p><strong>Последнее обновление:</strong> {{last_update}}</p>
                </div>
                <h2>Список документов в архиве</h2>
                {{documents_list}}
            </body>
            </html>
            """;
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
            .Replace("{{total_documents}}", _state.TotalDocuments.ToString())
            .Replace("{{last_update}}", _state.LastUpdate.ToString("yyyy-MM-dd HH:mm:ss"))
            .Replace("{{documents_list}}", documentsHtml.ToString());
        
        return html;
    }
}