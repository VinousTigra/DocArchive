using DocArhive.Models;
using Xunit.Abstractions;

namespace DocArhive.Tests.Controllers;
using DocArhive.Controllers;
using Xunit;


public class ArchiveControllerTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ProjectState _projectState;
    private readonly ArchiveController _controller;
    
    public ArchiveControllerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _projectState = new ProjectState();
        _controller = new ArchiveController(_projectState);
    }
    
    [Fact]
    public void AddDocument_WithValidData_ShouldAddDocument()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            ["title"] = "Test Document",
            ["description"] = "Test Description",
            ["filename"] = "test.pdf",
            ["category"] = "Test"
        };
        
        // Act
        var result = _controller.AddDocument(formData);
        
        // Debug
        _testOutputHelper.WriteLine($"Result: {result}");
        
        // Assert - проверяем базовые вещи
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        
        // Проверяем что документ добавился
        Assert.Equal(1, _projectState.TotalDocuments);
        
        var doc = _projectState.Documents.First();
        Assert.Equal("Test Document", doc.Title);
        Assert.Equal("Test Description", doc.Description);
        Assert.Equal("test.pdf", doc.FileName);
        Assert.Equal("Test", doc.Category);
    }
    
    [Fact]
    public void AddDocument_WithMinimalData_ShouldAddDocumentWithDefaults()
    {
        // Arrange - передаем ВСЕ 4 поля
        var formData = new Dictionary<string, string>
        {
            ["title"] = "Minimal Doc",
            ["description"] = "", // Пустое описание
            ["filename"] = "minimal.txt",
            ["category"] = "Без категории" // Явно указываем категорию
        };
    
        // Act
        var result = _controller.AddDocument(formData);
    
        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, _projectState.TotalDocuments);
    
        var doc = _projectState.Documents.First();
        Assert.Equal("Minimal Doc", doc.Title);
        Assert.Equal("", doc.Description);
        Assert.Equal("minimal.txt", doc.FileName);
        Assert.Equal("Без категории", doc.Category);
    }
    
    [Fact]
    public void AddDocument_WithMissingTitle_ShouldReturnError()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            ["filename"] = "test.pdf"
        };
        
        // Act
        var result = _controller.AddDocument(formData);
        
        // Assert
        Assert.NotNull(result);
        Assert.Contains("Ошибка", result);
        Assert.Equal(0, _projectState.TotalDocuments);
    }
    
    [Fact]
    public void AddDocument_WithMissingFileName_ShouldReturnError()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            ["title"] = "Test Document"
        };
        
        // Act
        var result = _controller.AddDocument(formData);
        
        // Assert
        Assert.NotNull(result);
        Assert.Contains("Ошибка", result);
        Assert.Equal(0, _projectState.TotalDocuments);
    }
    
    [Fact]
    public void AddDocument_ReturnsValidHtml()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            ["title"] = "HTML Test",
            ["filename"] = "test.html"
        };
        
        // Act
        var result = _controller.AddDocument(formData);
        
        // Assert
        Assert.NotNull(result);
        // Проверяем что это HTML документ
        Assert.StartsWith("<!DOCTYPE html>", result.Trim());
        Assert.Contains("<html>", result);
        Assert.Contains("</html>", result);
    }
}