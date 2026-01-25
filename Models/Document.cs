namespace DocArhive.Models;

public class Document
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string FileName { get; set; }
    public DateTime UploadDate { get; set; }
    public string Category { get; set; }
    
    // Используем современный синтаксис инициализации
    public Document(string title, string description, string fileName, string category)
    {
        Title = title;
        Description = description;
        FileName = fileName;
        Category = category;
        UploadDate = DateTime.Now;
    }
}