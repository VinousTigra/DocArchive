using DocArhive.Models;
using Xunit;

namespace DocArhive.Tests.Models;

public class DocumentTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var title = "Annual Report";
        var description = "Yearly financial report";
        var fileName = "report.pdf";
        var category = "Finance";
        
        // Act
        var document = new Document(title, description, fileName, category);
        
        // Assert
        Assert.Equal(title, document.Title);
        Assert.Equal(description, document.Description);
        Assert.Equal(fileName, document.FileName);
        Assert.Equal(category, document.Category);
        Assert.Equal(0, document.Id); // Не установлено до добавления в ProjectState
        Assert.True(document.UploadDate <= DateTime.Now);
        Assert.True(document.UploadDate >= DateTime.Now.AddSeconds(-1));
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Constructor_WithNullOrEmptyTitle_ThrowsArgumentNullException(string title)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new Document(title, "Description", "file.txt", "Category"));
    }
}