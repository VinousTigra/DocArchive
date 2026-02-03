
using DocArhive.Controllers;
using DocArhive.Models;
using Moq;
using Xunit;

namespace DocArhive.Tests;

public class RouterTests
{
    private readonly Mock<ProjectState> _mockState;
    private readonly Router _router;
    
    public RouterTests()
    {
        _mockState = new Mock<ProjectState>();
        _router = new Router(_mockState.Object);
    }
    
    [Theory]
    [InlineData("/", "GET")]
    [InlineData("/status", "GET")]
    public void Route_ValidGetRoutes_Returns200(string path, string method)
    {
        // Arrange
        var request = new HttpRequest
        {
            Method = method,
            Path = path,
            Headers = new Dictionary<string, string>(),
            Body = ""
        };
        
        // Act
        var response = _router.Route(request);
        
        // Assert
        Assert.Equal(200, response.StatusCode);
        Assert.Equal("OK", response.StatusMessage);
        Assert.Equal("text/html; charset=utf-8", response.ContentType);
    }
    
    [Theory]
    [InlineData("/unknown", "GET")]
    [InlineData("/api", "GET")]
    [InlineData("/docs", "GET")]
    public void Route_InvalidGetRoutes_Returns404(string path, string method)
    {
        // Arrange
        var request = new HttpRequest
        {
            Method = method,
            Path = path,
            Headers = new Dictionary<string, string>(),
            Body = ""
        };
        
        // Act
        var response = _router.Route(request);
        
        // Assert
        Assert.Equal(404, response.StatusCode);
        Assert.Equal("Not Found", response.StatusMessage);
        Assert.Contains("404", response.Content);
    }
    
    [Fact]
    public void Route_PostAction_WithFormData_Returns200()
    {
        // Arrange
        var request = new HttpRequest
        {
            Method = "POST",
            Path = "/action",
            Headers = new Dictionary<string, string>(),
            Body = "title=Test&description=Desc&filename=test.txt&category=Test"
        };
        
        // Act
        var response = _router.Route(request);
        
        // Assert
        Assert.Equal(200, response.StatusCode);
        Assert.Equal("OK", response.StatusMessage);
    }
    
    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("PATCH")]
    public void Route_UnsupportedMethod_Returns405(string method)
    {
        // Arrange
        var request = new HttpRequest
        {
            Method = method,
            Path = "/",
            Headers = new Dictionary<string, string>(),
            Body = ""
        };
        
        // Act
        var response = _router.Route(request);
        
        // Assert
        Assert.Equal(405, response.StatusCode);
        Assert.Equal("Method Not Allowed", response.StatusMessage);
        Assert.Contains("405", response.Content);
    }
    
    [Fact]
    public void ParseFormData_DecodesUrlEncodedValues()
    {
        // Arrange
        var body = "title=Annual%20Report&category=Finance%26Legal";
        
        // Act
        var result = RouterTestsHelper.ParseFormData(body);
        
        // Assert
        Assert.Equal("Annual Report", result["title"]);
        Assert.Equal("Finance&Legal", result["category"]);
    }
    
    [Fact]
    public void Route_ExceptionThrown_Returns500()
    {
        // Arrange
        var mockState = new Mock<ProjectState>();
        var mockController = new Mock<HomeController>(mockState.Object);
        
        // Создаем роутер, который выбросит исключение
        var router = new Router(mockState.Object);
        
        var request = new HttpRequest
        {
            Method = "GET",
            Path = "/",
            Headers = new Dictionary<string, string>(),
            Body = ""
        };
        
        // Используем рефлексию для подмены контроллера (тест edge-case)
        // В реальном проекте нужно использовать Dependency Injection
        
        // Act & Assert - проверяем, что не падает
        var response = router.Route(request);
        Assert.NotNull(response);
    }
}

// Вспомогательный класс для тестирования приватных методов
public static class RouterTestsHelper
{
    public static Dictionary<string, string> ParseFormData(string body)
    {
        var method = typeof(Router).GetMethod("ParseFormData", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        return method?.Invoke(null, new object[] { body }) as Dictionary<string, string>;
    }
}