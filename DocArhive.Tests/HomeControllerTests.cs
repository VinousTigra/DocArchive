namespace DocArhive.Tests.Controllers;

using System.Text;
using DocArhive.Controllers;
using FluentAssertions;
using DocArhive.Models;
using Moq;
using Xunit;


public class HomeControllerTests : IDisposable
{
    private readonly Mock<ProjectState> _mockProjectState;
    private readonly string _tempViewsPath;
    private readonly HomeController _controller;
    
    public HomeControllerTests()
    {
        _mockProjectState = new Mock<ProjectState>();
        _tempViewsPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempViewsPath);
        
        // Create test view files
        File.WriteAllText(Path.Combine(_tempViewsPath, "index.html"), 
            "<html><body>Test Index</body></html>", Encoding.UTF8);
        
        File.WriteAllText(Path.Combine(_tempViewsPath, "status.html"), 
            "<html><body>Total: {{total_documents}}, Last: {{last_update}}, List: {{documents_list}}</body></html>", 
            Encoding.UTF8);
        
        _controller = new HomeController(_mockProjectState.Object);
        
        // Use reflection to set private field for testing
        var field = typeof(HomeController).GetField("_viewsPath", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(_controller, _tempViewsPath);
    }
    
    public void Dispose()
    {
        if (Directory.Exists(_tempViewsPath))
        {
            Directory.Delete(_tempViewsPath, true);
        }
    }
    
    [Fact]
    public void Index_ShouldReturnViewContent()
    {
        // Act
        var result = _controller.Index();
        
        // Assert
        result.Should().Contain("Test Index");
    }
    
    [Fact]
    public void Status_WithNoDocuments_ShouldShowEmptyMessage()
    {
        // Arrange
        _mockProjectState.Setup(x => x.Documents).Returns(new List<Document>());
        _mockProjectState.Setup(x => x.TotalDocuments).Returns(0);
        _mockProjectState.Setup(x => x.LastUpdate).Returns(DateTime.Now);
        
        // Act
        var result = _controller.Status();
        
        // Assert
        result.Should().Contain("Total: 0");
        result.Should().Contain("Не загружено ни одного документа");
    }
    
    [Fact]
    public void Status_WithDocuments_ShouldRenderTable()
    {
        // Arrange
        var documents = new List<Document>
        {
            new Document("Doc 1", "Description 1", "file1.pdf", "Category 1"),
            new Document("Doc 2", "Description 2", "file2.pdf", "Category 2")
        };
        
        documents[0].Id = 1;
        documents[1].Id = 2;
        
        _mockProjectState.Setup(x => x.Documents).Returns(documents);
        _mockProjectState.Setup(x => x.TotalDocuments).Returns(2);
        _mockProjectState.Setup(x => x.LastUpdate).Returns(new DateTime(2024, 1, 1, 12, 0, 0));
        
        // Act
        var result = _controller.Status();
        
        // Assert
        result.Should().Contain("Total: 2");
        result.Should().Contain("2024-01-01 12:00");
        result.Should().Contain("Doc 1");
        result.Should().Contain("Doc 2");
        result.Should().Contain("<table>");
    }
    
    [Fact]
    public void Index_WhenViewFileMissing_ShouldReturnFileNameAsFallback()
    {
        // Arrange - delete the view file
        File.Delete(Path.Combine(_tempViewsPath, "index.html"));
        
        // Act
        var result = _controller.Index();
        
        // Assert
        result.Should().Be("index.html");
    }
}