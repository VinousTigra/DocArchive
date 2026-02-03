using DocArhive.Controllers;
using DocArhive.Models;
using Moq;
using System.Text;
using Xunit;

namespace DocArhive.Tests.Controllers;

public class HomeControllerTests : IDisposable
{
    private readonly Mock<ProjectState> _mockState;
    private readonly string _tempViewsPath;
    private readonly HomeController _controller;
    
    public HomeControllerTests()
    {
        _mockState = new Mock<ProjectState>();
        
        // Создаем временную директорию для тестовых Views
        _tempViewsPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempViewsPath);
        
        // Создаем тестовые HTML файлы
        File.WriteAllText(Path.Combine(_tempViewsPath, "index.html"), 
            "<h1>Test Index</h1>");
        File.WriteAllText(Path.Combine(_tempViewsPath, "status.html"), 
            "<h1>Status</h1><p>Total: {{total_documents}}</p><p>Last: {{last_update}}</p><div>{{documents_list}}</div>");
        
        // Используем рефлексию для установки пути к Views
        _controller = CreateControllerWithCustomViewsPath();
    }
    
    private HomeController CreateControllerWithCustomViewsPath()
    {
        var state = _mockState.Object;
        var controller = new HomeController(state);
        
        // Используем рефлексию для установки пути к Views
        var field = typeof(HomeController).GetField("_viewsPath", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(controller, _tempViewsPath);
        
        return controller;
    }
    
    [Fact]
    public void Index_ReturnsIndexHtmlContent()
    {
        // Act
        var result = _controller.Index();
        
        // Assert
        Assert.Contains("Test Index", result);
    }
    
    [Fact]
    public void Status_WithNoDocuments_ReturnsProperHtml()
    {
        // Arrange
        _mockState.Setup(s => s.TotalDocuments).Returns(0);
        _mockState.Setup(s => s.LastUpdate).Returns(new DateTime(2024, 1, 1));
        _mockState.Setup(s => s.Documents).Returns(new List<Document>());
        
        // Act
        var result = _controller.Status();
        
        // Assert
        Assert.Contains("Total: 0", result);
        Assert.Contains("2024-01-01", result);
        Assert.Contains("documents_list", result);
    }
    
    [Fact]
    public void Status_WithDocuments_IncludesTable()
    {
        // Arrange
        var documents = new List<Document>
        {
            new Document("Doc1", "Desc1", "file1.txt", "Finance") { Id = 1, UploadDate = new DateTime(2024, 1, 1) },
            new Document("Doc2", "Desc2", "file2.pdf", "Legal") { Id = 2, UploadDate = new DateTime(2024, 1, 2) }
        };
        
        _mockState.Setup(s => s.TotalDocuments).Returns(2);
        _mockState.Setup(s => s.LastUpdate).Returns(new DateTime(2024, 1, 2));
        _mockState.Setup(s => s.Documents).Returns(documents);
        
        // Act
        var result = _controller.Status();
        
        // Assert
        Assert.Contains("Doc1", result);
        Assert.Contains("Doc2", result);
        Assert.Contains("<table>", result);
        Assert.Contains("Finance", result);
        Assert.Contains("Legal", result);
    }
    
    [Fact]
    public void Index_FileNotFound_ReturnsFallback()
    {
        // Arrange - удаляем файл
        File.Delete(Path.Combine(_tempViewsPath, "index.html"));
        var controller = CreateControllerWithCustomViewsPath();
        
        // Act
        var result = controller.Index();
        
        // Assert
        Assert.Equal("index.html", result); // Fallback возвращает имя файла
    }
    
    public void Dispose()
    {
        // Очистка временных файлов
        if (Directory.Exists(_tempViewsPath))
        {
            Directory.Delete(_tempViewsPath, true);
        }
    }
}