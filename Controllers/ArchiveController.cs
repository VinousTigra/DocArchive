using System.Collections.Generic;
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
        if (!formData.ContainsKey("title") || !formData.ContainsKey("description") || 
            !formData.ContainsKey("filename") || !formData.ContainsKey("category"))
        {
            return """
                   <!DOCTYPE html>
                   <html>
                   <body>
                       <h1>Ошибка</h1>
                       <p>Не все поля заполнены</p>
                       <a href="/">Вернуться на главную</a>
                   </body>
                   </html>
                   """;
        }
        
        var document = new Document(
            formData["title"],
            formData["description"],
            formData["filename"],
            formData["category"]
        );
        
        _state.AddDocument(document);
        
        return """
               <!DOCTYPE html>
               <html>
               <head><title>Документ добавлен</title></head>
               <body>
                   <h1>Документ успешно добавлен в архив!</h1>
                   <p><a href='/'>Вернуться на главную</a></p>
                   <p><a href='/status'>Просмотреть архив</a></p>
               </body>
               </html>
               """;
    }
}