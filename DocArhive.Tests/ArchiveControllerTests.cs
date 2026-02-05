using DocArhive.Controllers;

namespace DocArhive.Tests.Controllers;

using FluentAssertions;
using DocArhive.Models;
using Moq;
using Xunit;

public class ArchiveControllerTests
{
    private readonly Mock<ProjectState> _mockProjectState;
    private readonly ArchiveController _controller;
    
    public ArchiveControllerTests()
    {
        _mockProjectState = new Mock<ProjectState>();
        _controller = new ArchiveController(_mockProjectState.Object);
    }
    
    [Fact]
    public void AddDocument_WithValidData_ShouldAddDocumentAndReturnSuccessResponse()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            ["title"] = "Annual Report 2024",
            ["description"] = "Financial report for 2024",
            ["filename"] = "report2024.pdf",
            ["category"] = "Finance"
        };
        
        Document capturedDocument = null;
        _mockProjectState.Setup(x => x.AddDocument(It.IsAny<Document>()))
            .Callback<Document>(doc => capturedDocument = doc);
        
        // Act
        var result = _controller.AddDocument(formData);
        
        // Assert
        result.Should().Contain("успешно добавлен");
        result.Should().Contain("Annual Report 2024");
        result.Should().Contain("<script>");
        result.Should().Contain("alert(");
        
        capturedDocument.Should().NotBeNull();
        capturedDocument!.Title.Should().Be("Annual Report 2024");
        capturedDocument.Description.Should().Be("Financial report for 2024");
        capturedDocument.FileName.Should().Be("report2024.pdf");
        capturedDocument.Category.Should().Be("Finance");
    }
    
    [Theory]
    [InlineData(null, "report.pdf", "Test")]
    [InlineData("", "report.pdf", "Test")]
    [InlineData("   ", "report.pdf", "Test")]
    [InlineData("Title", null, "Test")]
    [InlineData("Title", "", "Test")]
    [InlineData("Title", "   ", "Test")]
    public void AddDocument_WithMissingRequiredFields_ShouldReturnErrorResponse(
        string title, string filename, string category)
    {
        // Arrange
        var formData = new Dictionary<string, string>();
        if (title != null) formData["title"] = title;
        if (filename != null) formData["filename"] = filename;
        if (category != null) formData["category"] = category;
        
        // Act
        var result = _controller.AddDocument(formData);
        
        // Assert
        result.Should().Contain("Ошибка");
        result.Should().Contain("обязательны");
        _mockProjectState.Verify(x => x.AddDocument(It.IsAny<Document>()), Times.Never);
    }
    
    [Fact]
    public void AddDocument_WithSpecialCharacters_ShouldEscapeForJavaScript()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            ["title"] = "O'Reilly's \"Special\" Report\nNew Line",
            ["filename"] = "report.pdf",
            ["category"] = "Test"
        };
        
        // Act
        var result = _controller.AddDocument(formData);
        
        // Assert
        result.Should().Contain("O\\'Reilly\\'s \\\"Special\\\" Report\\nNew Line");
    }
    
    [Fact]
    public void AddDocument_WithMinimalData_ShouldUseDefaults()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            ["title"] = "Minimal Doc",
            ["filename"] = "minimal.txt"
            // No description or category
        };
        
        Document capturedDocument = null;
        _mockProjectState.Setup(x => x.AddDocument(It.IsAny<Document>()))
            .Callback<Document>(doc => capturedDocument = doc);
        
        // Act
        var result = _controller.AddDocument(formData);
        
        // Assert
        capturedDocument.Should().NotBeNull();
        capturedDocument!.Description.Should().BeEmpty();
        capturedDocument.Category.Should().Be("Без категории");
    }
}