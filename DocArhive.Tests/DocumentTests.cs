namespace DocArhive.Tests.Models;

using FluentAssertions;
using Xunit;
using DocArhive.Models;

public class DocumentTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateDocument()
    {
        // Arrange
        const string title = "Test Document";
        const string description = "Test Description";
        const string fileName = "test.pdf";
        const string category = "Reports";

        // Act
        var document = new Document(title, description, fileName, category);

        // Assert
        document.Title.Should().Be(title);
        document.Description.Should().Be(description);
        document.FileName.Should().Be(fileName);
        document.Category.Should().Be(category);
        document.UploadDate.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        document.Id.Should().Be(0); // ID should be set by ProjectState
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidTitle_ShouldThrowException(string invalidTitle)
    {
        // Arrange & Act
        Action act = () => new Document(invalidTitle, "Description", "file.txt", "Category");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*title*");
    }
}