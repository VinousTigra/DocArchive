namespace DocArhive.Tests.Models;

using DocArhive.Models;
using FluentAssertions;
using Xunit;
public class ProjectStateTests
{
    private readonly ProjectState _projectState;
    
    public ProjectStateTests()
    {
        _projectState = new ProjectState();
    }
    
    [Fact]
    public void AddDocument_ShouldIncrementIdAndAddDocument()
    {
        // Arrange
        var document1 = new Document("Doc 1", "Description", "file1.pdf", "Category1");
        var document2 = new Document("Doc 2", "Description", "file2.pdf", "Category2");
        
        // Act
        _projectState.AddDocument(document1);
        var id1 = document1.Id;
        _projectState.AddDocument(document2);
        var id2 = document2.Id;
        
        // Assert
        id1.Should().BeGreaterThan(0);
        id2.Should().BeGreaterThan(id1); // Каждый следующий ID больше предыдущего
        _projectState.TotalDocuments.Should().Be(2);
        _projectState.LastUpdate.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
    }
    
    [Fact]
    public void GetDocument_WithExistingId_ShouldReturnDocument()
    {
        // Arrange
        var document = new Document("Test", "Desc", "file.pdf", "Cat");
        _projectState.AddDocument(document);
        var addedDocumentId = document.Id; // Сохраняем реальный ID
        
        // Act
        var result = _projectState.GetDocument(addedDocumentId);
        
        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Test");
        result.Id.Should().Be(addedDocumentId); // Проверяем, что ID совпадает
    }
    
    [Fact]
    public void GetDocument_WithNonExistingId_ShouldReturnNull()
    {
        // Act
        var result = _projectState.GetDocument(999);
        
        // Assert
        result.Should().BeNull();
    }
    
    [Fact]
    public void RemoveDocument_WithExistingId_ShouldRemoveDocument()
    {
        // Arrange
        var document = new Document("Test", "Desc", "file.pdf", "Cat");
        _projectState.AddDocument(document);
        var addedDocumentId = document.Id; // Сохраняем реальный ID
        
        // Act
        var result = _projectState.RemoveDocument(addedDocumentId);
        
        // Assert
        result.Should().BeTrue();
        _projectState.TotalDocuments.Should().Be(0);
        _projectState.GetDocument(addedDocumentId).Should().BeNull();
    }
    
    [Fact]
    public void GetDocumentsByCategory_ShouldReturnFilteredDocuments()
    {
        // Arrange
        _projectState.AddDocument(new Document("Doc 1", "Desc", "f1.pdf", "Finance"));
        _projectState.AddDocument(new Document("Doc 2", "Desc", "f2.pdf", "HR"));
        _projectState.AddDocument(new Document("Doc 3", "Desc", "f3.pdf", "Finance"));
        
        // Act
        var financeDocs = _projectState.GetDocumentsByCategory("Finance");
        
        // Assert
        financeDocs.Should().HaveCount(2);
        financeDocs.All(d => d.Category == "Finance").Should().BeTrue();
    }
    
    [Fact]
    public void Documents_Property_ShouldReturnAllDocuments()
    {
        // Arrange
        _projectState.AddDocument(new Document("Doc 1", "Desc", "f1.pdf", "Cat1"));
        _projectState.AddDocument(new Document("Doc 2", "Desc", "f2.pdf", "Cat2"));
        
        // Act
        var allDocs = _projectState.Documents;
        
        // Assert
        allDocs.Should().HaveCount(2);
        allDocs.Should().Contain(d => d.Title == "Doc 1");
        allDocs.Should().Contain(d => d.Title == "Doc 2");
    }
}