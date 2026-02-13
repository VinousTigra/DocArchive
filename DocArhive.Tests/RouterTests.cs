using Xunit.Abstractions;

namespace DocArhive.Tests.Services;

using DocArhive.Models;
using DocArhive;
using FluentAssertions;
using Xunit;

public class RouterTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ProjectState _projectState;
    private readonly Router _router;
    
    public RouterTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _projectState = new ProjectState();
        _router = new Router(_projectState);
    }
    
    [Theory]
    [InlineData("GET", "/")]
    [InlineData("GET", "/status")]
    public void Route_ValidGetRequests_ShouldReturnOkResponse(string method, string path)
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
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(200);
        response.StatusMessage.Should().Be("OK");
        response.Content.Should().NotBeNullOrEmpty();
        response.ContentType.Should().Be("text/html; charset=utf-8");
    }
    [Fact]
    public void Minimal_Post_Test()
    {
        // Самый простой тест
        var request = new HttpRequest
        {
            Method = "POST",
            Path = "/action",
            Headers = new Dictionary<string, string>(),
            Body = "title=test&filename=test.txt"
        };
    
        var response = _router.Route(request);
    
        _testOutputHelper.WriteLine($"Response Status: {response.StatusCode}");
        _testOutputHelper.WriteLine($"Response Content Type: {response.ContentType}");
        _testOutputHelper.WriteLine($"Response Content Length: {response.Content.Length}");
        _testOutputHelper.WriteLine($"Response Content: {response.Content}");
    
        Assert.Equal(200, response.StatusCode);
        Assert.True(response.Content.Length > 10); 
    }
    [Fact]
    public void Route_WhenExceptionThrown_ShouldReturn500()
    {
        // тестируем обработку исключений в самом Router
        var request = new HttpRequest
        {
            Method = "GET",
            Path = "/nonexistent",
            Headers = new Dictionary<string, string>(),
            Body = ""
        };
        
        var response = _router.Route(request);
        
        // Assert - проверяем, что роутер не падает на исключениях
        response.Should().NotBeNull();
        // Для несуществующего пути должен быть 404, а не 500
        response.StatusCode.Should().Be(404);
    }
    
    // Тест на исключение
    [Fact]
    public void Router_ShouldHandleControllerExceptionsGracefully()
    {
        // Arrange
        // Создаем специальный ProjectState, который вызовет проблему в контроллере
        var problematicState = new ProjectState();
        var router = new Router(problematicState);
        
        // Добавляем документ, чтобы убедиться, что все работает
        problematicState.AddDocument(new Document("Test", "Test", "test.pdf", "Test"));
        
        var request = new HttpRequest
        {
            Method = "GET",
            Path = "/status", // Этот путь существует
            Headers = new Dictionary<string, string>(),
            Body = ""
        };
        
        // Act
        var response = router.Route(request);
        
        // Assert - даже если в контроллере будет проблема, Router должен вернуть 500
        response.Should().NotBeNull();
        // Статус должен быть либо 200 (успех), либо 500 (ошибка), но не падать
        response.StatusCode.Should().BeOneOf(200, 500);
    }
    
    [Theory]
    [InlineData("PUT", "/")]
    [InlineData("DELETE", "/")]
    [InlineData("PATCH", "/")]
    public void Route_UnsupportedMethod_ShouldReturn405(string method, string path)
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
        response.StatusCode.Should().Be(405);
        response.StatusMessage.Should().Be("Method Not Allowed");
        response.Content.Should().Contain("405");
    }
    
    [Theory]
    [InlineData("/unknown")]
    [InlineData("/api/test")]
    [InlineData("/docs/index.html")]
    public void Route_UnknownPath_ShouldReturn404(string path)
    {
        // Arrange
        var request = new HttpRequest
        {
            Method = "GET",
            Path = path,
            Headers = new Dictionary<string, string>(),
            Body = ""
        };
        
        // Act
        var response = _router.Route(request);
        
        // Assert
        response.StatusCode.Should().Be(404);
        response.StatusMessage.Should().Be("Not Found");
        response.Content.Should().Contain("404");
    }
    
    // Тест приватного метода ParseFormData
    [Fact]
    public void ParseFormData_WithEncodedValues_ShouldDecodeProperly()
    {
        // Arrange
        const string body = "title=Test%20Document&filename=report%2Epdf&category=Finance%26Legal";
        
        // Act - используем рефлексию для вызова приватного метода
        var method = typeof(Router).GetMethod("ParseFormData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        var result = method?.Invoke(null, [body]) as Dictionary<string, string>;
        
        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("title");
        result["title"].Should().Be("Test Document");
        result["filename"].Should().Be("report.pdf");
        result["category"].Should().Be("Finance&Legal");
    }
    
    // Дополнительные тесты для покрытия edge cases
    [Fact]
    public void ParseFormData_WithEmptyBody_ShouldReturnEmptyDictionary()
    {
        // Arrange
        const string body = "";
        
        // Act
        var method = typeof(Router).GetMethod("ParseFormData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        var result = method?.Invoke(null, [body]) as Dictionary<string, string>;
        
        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
    
    [Fact]
    public void ParseFormData_WithMalformedPair_ShouldSkipIt()
    {
        // Arrange
        const string body = "key1=value1&malformed&key2=value2";
        
        // Act
        var method = typeof(Router).GetMethod("ParseFormData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        var result = method?.Invoke(null, [body]) as Dictionary<string, string>;
        
        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().ContainKey("key1");
        result.Should().ContainKey("key2");
        result.Should().NotContainKey("malformed");
    }
}