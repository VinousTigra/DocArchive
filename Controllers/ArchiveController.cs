using System.Collections.Generic;
using System.Net;
using DocArhive.Models;

namespace DocArhive.Controllers;

public class ArchiveController
{
    private readonly ProjectState _state;

    public ArchiveController(ProjectState state)
    {
        _state = state;
    }

    public string AddDocument(Dictionary<string, string> formData)
    {
        // Валидация
        if (!formData.ContainsKey("title") || string.IsNullOrWhiteSpace(formData["title"]) ||
            !formData.ContainsKey("filename") || string.IsNullOrWhiteSpace(formData["filename"]))
            return GetErrorResponse("Название документа и имя файла обязательны для заполнения");

        // Создание документа
        var document = new Document(
            formData["title"],
            formData.TryGetValue("description", out var desc) ? desc : "",
            formData["filename"],
            formData.TryGetValue("category", out var cat) ? cat : "Без категории"
        );

        // Добавление в архив
        _state.AddDocument(document);

        // Возвращаем HTML
        return GetSuccessResponse(document);
    }

    private string GetErrorResponse(string message)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <title>Ошибка</title>
    <script>
        // Показываем alert с ошибкой
        alert('Ошибка: {WebUtility.HtmlEncode(message)}');
        // Возвращаем на главную страницу
        window.location.href = '/';
    </script>
</head>
<body>
    <p>Если перенаправление не произошло, <a href='/'>нажмите сюда</a>.</p>
</body>
</html>";
    }

    private string GetSuccessResponse(Document document)
    {
        return $@"
<!DOCTYPE html>
<html>

<head>
    <style>
        body {{ font-family: cursive; margin: 40px; }}
        a {{ display: inline-block; padding: 10px 20px; background: #538f46; color: white; text-decoration: none; margin: 5px; border-radius: 4px; }}
        a:hover {{ background: #d02f66; }}
        table {{ border-collapse: collapse; width: 100%; margin: 20px 0; }}
        th, td {{ border: 1px solid #ddd; padding: 12px; text-align: left; }}
        th {{ background-color: #303030; color: white; }}
    </style>
    <title>Успешно</title>
    <script>
        // Показываем alert с подтверждением
        //alert('Документ ""{EscapeForJavaScript(WebUtility.HtmlEncode(document.Title))}"" успешно добавлен!');
     
        // Перенаправляем на главную страницу
         setTimeout(function() {{
            window.location.href = '/status';
        }}, 3000);    

    </script>
</head>
<body>
    <h1>Документ успешно загружен!</h1>    
    <p>Если перенаправление не произошло</p><a href='/status'>нажмите сюда</a>
</body>
</html>";
    }

    private string EscapeForJavaScript(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        // Экранируем специальные символы для JavaScript
        return text
            .Replace("\\", "\\\\") // обратный слеш
            .Replace("'", "\\'") // одинарная кавычка
            .Replace("\"", "\\\"") // двойная кавычка
            .Replace("\n", "\\n") // новая строка
            .Replace("\r", "\\r"); // возврат каретки
    }
}