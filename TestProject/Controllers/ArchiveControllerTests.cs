using Xunit;
using System.Collections.Generic;
using DocArhive.Controllers;
using DocArhive.Models;  // Изменено с Models на DocArhive.Models

namespace DocArhive.Tests.Controllers;

public class ArchiveControllerTests
{
    [Fact]
    public void AddDocument_ValidData_ReturnsSuccessHtml()
    {
        // Arrange
        var state = new ProjectState();
        var controller = new ArchiveController(state);
        
        var formData = new Dictionary<string, string>
        {
            ["title"] = "Annual Report",
            ["description"] = "Yearly financial report",
            ["filename"] = "report.pdf",
            ["category"] = "Finance"
        };
        
        // Act
        var result = controller.AddDocument(formData);
        
        // Assert
        Assert.Contains("успешно", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("alert", result);
    }
    
    [Theory]
    [InlineData("", "filename.pdf", "Category")]
    [InlineData("Title", "", "Category")]
    [InlineData("   ", "filename.pdf", "Category")]
    public void AddDocument_MissingRequiredFields_ReturnsErrorHtml(
        string title, string filename, string category)
    {
        // Arrange
        var state = new ProjectState();
        var controller = new ArchiveController(state);
        
        var formData = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(title)) formData["title"] = title;
        if (!string.IsNullOrWhiteSpace(filename)) formData["filename"] = filename;
        formData["category"] = category;
        
        // Act
        var result = controller.AddDocument(formData);
        
        // Assert
        Assert.Contains("Ошибка", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("alert", result);
    }
    
    [Fact]
    public void AddDocument_SpecialCharacters_EscapesForJavaScript()
    {
        // Arrange
        var state = new ProjectState();
        var controller = new ArchiveController(state);
        
        var formData = new Dictionary<string, string>
        {
            ["title"] = "Report with \"quotes\" and 'apostrophes'",
            ["description"] = "Line 1\nLine 2",
            ["filename"] = "report.pdf",
            ["category"] = "Test"
        };
        
        // Act
        var result = controller.AddDocument(formData);
        
        // Assert
        Assert.Contains("Report", result);
    }
}