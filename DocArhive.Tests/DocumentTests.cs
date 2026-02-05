namespace DocArhive.Tests.Models;

using FluentAssertions;
using Xunit;
using DocArhive.Models;

public class DocumentTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidTitle_ShouldThrowException(string? invalidTitle)
    {
        // Arrange & Act
        Action act = () => new Document(invalidTitle, "Description", "file.txt", "Category");
    
        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Title cannot be null, empty or whitespace*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidFileName_ShouldThrowException(string? invalidFileName)
    {
        // Arrange & Act
        Action act = () => new Document("Valid Title", "Description", invalidFileName, "Category");
    
        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*FileName cannot be null, empty or whitespace*");
    }
}