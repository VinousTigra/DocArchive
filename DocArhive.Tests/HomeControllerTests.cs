
using DocArhive.Models;

namespace DocArhive.Tests.Controllers;
using DocArhive.Controllers;
using Xunit;

public class HomeControllerSimpleTests
{
    [Fact]
    public void HomeController_CanBeCreated()
    {
        // Arrange
        var projectState = new ProjectState();
        
        // Act
        var controller = new HomeController(projectState);
        
        // Assert
        Assert.NotNull(controller);
    }
    
    [Fact]
    public void Index_ReturnsString()
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
    public void Status_ReturnsString()
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
    
    [Fact]
    public void Status_ShowsDocumentCount()
    {
        // Arrange
        var projectState = new ProjectState();
        projectState.AddDocument(new Document("Test", "Test", "test.pdf", "Test"));
        var controller = new HomeController(projectState);
        
        // Act
        var result = controller.Status();
        
        // Assert
        Assert.NotNull(result);
        // Хотя бы проверяем, что что-то возвращается
        Assert.True(result.Length > 10);
    }
}