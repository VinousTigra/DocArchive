using DocArhive.Models;

namespace DocArhive.Tests.Controllers;

using System.Text;
using DocArhive.Controllers;
using Xunit;


public class HomeControllerTests : IDisposable
{
    private readonly ProjectState _projectState;
    private readonly string _tempProjectRoot;
    private readonly string _viewsPath;
    private readonly HomeController _controller;
    
    public HomeControllerTests()
    {
        _projectState = new ProjectState();
        
        // Создаем временную структуру папок
        _tempProjectRoot = Path.Combine(Path.GetTempPath(), $"TestProject_{Guid.NewGuid()}");
        _viewsPath = Path.Combine(_tempProjectRoot, "Views");
        
        Directory.CreateDirectory(_tempProjectRoot);
        Directory.CreateDirectory(_viewsPath);
        
        // Сохраняем оригинальную текущую директорию
        var originalDir = Directory.GetCurrentDirectory();
        
        try
        {
            // Устанавливаем временную директорию как текущую
            Directory.SetCurrentDirectory(_tempProjectRoot);
            
            // Создаем тестовые файлы представлений
            File.WriteAllText(Path.Combine(_viewsPath, "index.html"), 
                @"<!DOCTYPE html>
                <html lang='ru'>
                <head>
                    <meta charset='utf-8'>
                    <title>Электронный архив документов</title>
                </head>
                <body>
                    <h1>Электронный архив документов</h1>
                    <p>Добро пожаловать в систему электронного архива!</p>
                </body>
                </html>", 
                Encoding.UTF8);
            
            File.WriteAllText(Path.Combine(_viewsPath, "status.html"), 
                @"<!DOCTYPE html>
                <html lang='ru'>
                <head>
                    <meta charset='utf-8'>
                    <title>Статус архива</title>
                </head>
                <body>
                    <h1>Статус электронного архива</h1>
                    <p>Всего документов: {{total_documents}}</p>
                    <p>Последнее обновление: {{last_update}}</p>
                    <h2>Список документов:</h2>
                    <div>{{documents_list}}</div>
                </body>
                </html>", 
                Encoding.UTF8);
            
            // Создаем контроллер (он будет использовать текущую директорию)
            _controller = new HomeController(_projectState);
        }
        finally
        {
            // Всегда восстанавливаем оригинальную директорию
            Directory.SetCurrentDirectory(originalDir);
        }
    }
    
    public void Dispose()
    {
        try
        {
            // Удаляем временную директорию
            if (Directory.Exists(_tempProjectRoot))
            {
                Directory.Delete(_tempProjectRoot, true);
            }
        }
        catch
        {
            // Игнорируем ошибки удаления (возможно, файлы все еще используются)
        }
    }
    
    [Fact]
    public void HomeController_CanBeCreated()
    {
        // Arrange & Act & Assert
        Assert.NotNull(_controller);
    }
    
    [Fact]
    public void Index_ReturnsHtmlContent()
    {
        // Arrange
        var originalDir = Directory.GetCurrentDirectory();
        
        try
        {
            Directory.SetCurrentDirectory(_tempProjectRoot);
            
            // Act
            var result = _controller.Index();
            
            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Contains("<!DOCTYPE html>", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Электронный архив документов", result, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }
    
    [Fact]
    public void Status_WithNoDocuments_ReturnsStatusPage()
    {
        // Arrange
        var originalDir = Directory.GetCurrentDirectory();
        
        try
        {
            Directory.SetCurrentDirectory(_tempProjectRoot);
            
            // Act
            var result = _controller.Status();
            
            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Contains("<!DOCTYPE html>", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Всего документов: 0", result);
            Assert.Contains("Не загружено ни одного документа", result);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }
    
    [Fact]
    public void Status_WithDocuments_ReturnsTableWithDocuments()
    {
        // Arrange
        var originalDir = Directory.GetCurrentDirectory();
        
        try
        {
            Directory.SetCurrentDirectory(_tempProjectRoot);
            
            // Добавляем тестовые документы
            _projectState.AddDocument(new Document("Годовой отчет 2023", "Финансовый отчет за 2023 год", "report_2023.pdf", "Финансы"));
            _projectState.AddDocument(new Document("План разработки", "План разработки на Q2 2024", "dev_plan.pdf", "Проекты"));
            
            // Act
            var result = _controller.Status();
            
            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Contains("<!DOCTYPE html>", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Всего документов: 2", result);
            Assert.Contains("Годовой отчет 2023", result);
            Assert.Contains("План разработки", result);
            Assert.Contains("<table>", result);
            Assert.Contains("<th>ID</th>", result);
            Assert.Contains("<th>Название</th>", result);
            Assert.Contains("<th>Категория</th>", result);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }
    
    [Fact]
    public void Index_WhenViewFileMissing_ReturnsFileNameAsFallback()
    {
        // Arrange
        var originalDir = Directory.GetCurrentDirectory();
        
        try
        {
            Directory.SetCurrentDirectory(_tempProjectRoot);
            
            // Удаляем файл представления
            File.Delete(Path.Combine(_viewsPath, "index.html"));
            
            // Act
            var result = _controller.Index();
            
            // Assert
            Assert.Equal("index.html", result);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }
    
    [Fact]
    public void Status_ReplacesTemplateVariables()
    {
        // Arrange
        var originalDir = Directory.GetCurrentDirectory();
        
        try
        {
            Directory.SetCurrentDirectory(_tempProjectRoot);
            
            // Добавляем документ
            var doc = new Document("Тестовый документ", "Описание", "test.pdf", "Тест");
            _projectState.AddDocument(doc);
            
            // Act
            var result = _controller.Status();
            
            // Assert
            Assert.DoesNotContain("{{total_documents}}", result);
            Assert.DoesNotContain("{{last_update}}", result);
            Assert.DoesNotContain("{{documents_list}}", result);
            Assert.Contains($"Всего документов: {_projectState.TotalDocuments}", result);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }
}

// Тесты для работы с файловой системой (отдельный класс)
public class HomeControllerFileSystemTests : IDisposable
{
    private readonly string _tempProjectRoot;
    private readonly string _viewsPath;
    
    public HomeControllerFileSystemTests()
    {
        _tempProjectRoot = Path.Combine(Path.GetTempPath(), $"TestProjectFS_{Guid.NewGuid()}");
        _viewsPath = Path.Combine(_tempProjectRoot, "Views");
        Directory.CreateDirectory(_tempProjectRoot);
        Directory.CreateDirectory(_viewsPath);
    }
    
    public void Dispose()
    {
        if (Directory.Exists(_tempProjectRoot))
        {
            Directory.Delete(_tempProjectRoot, true);
        }
    }
    
    [Fact]
    public void ReadViewFile_ReturnsFileContent_WhenFileExists()
    {
        // Arrange
        var originalDir = Directory.GetCurrentDirectory();
        
        try
        {
            Directory.SetCurrentDirectory(_tempProjectRoot);
            
            var testContent = "<!DOCTYPE html><html><body>Test</body></html>";
            File.WriteAllText(Path.Combine(_viewsPath, "test.html"), testContent, Encoding.UTF8);
            
            var projectState = new ProjectState();
            var controller = new HomeController(projectState);
            
            // Используем рефлексию для вызова приватного метода
            var method = typeof(HomeController).GetMethod("ReadViewFile", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // Act
            var result = method?.Invoke(controller, new object[] { "test.html" }) as string;
            
            // Assert
            Assert.Equal(testContent, result);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }
    
    [Fact]
    public void ReadViewFile_ReturnsFileName_WhenFileMissing()
    {
        // Arrange
        var originalDir = Directory.GetCurrentDirectory();
        
        try
        {
            Directory.SetCurrentDirectory(_tempProjectRoot);
            
            var projectState = new ProjectState();
            var controller = new HomeController(projectState);
            
            // Используем рефлексию для вызова приватного метода
            var method = typeof(HomeController).GetMethod("ReadViewFile", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // Act
            var result = method?.Invoke(controller, new object[] { "nonexistent.html" }) as string;
            
            // Assert
            Assert.Equal("nonexistent.html", result);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }
}

// Упрощенные тесты без файловой системы (для быстрой проверки логики)
public class HomeControllerUnitTests
{
    [Fact]
    public void HomeController_Constructor_InitializesProperties()
    {
        // Arrange
        var projectState = new ProjectState();
        
        // Act
        var controller = new HomeController(projectState);
        
        // Assert
        Assert.NotNull(controller);
    }
    
    [Fact]
    public void Index_MethodExists()
    {
        // Arrange
        var projectState = new ProjectState();
        var controller = new HomeController(projectState);
        
        // Act
        var result = controller.Index();
        
        // Assert
        Assert.NotNull(result);
        Assert.IsType<string>(result);
    }
    
    [Fact]
    public void Status_MethodExists()
    {
        // Arrange
        var projectState = new ProjectState();
        var controller = new HomeController(projectState);
        
        // Act
        var result = controller.Status();
        
        // Assert
        Assert.NotNull(result);
        Assert.IsType<string>(result);
    }
}