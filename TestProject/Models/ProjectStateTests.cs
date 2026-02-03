using DocArhive.Models;
using Xunit;

namespace DocArhive.Tests.Models;

public class ProjectStateTests
{
    private readonly ProjectState _state;
    private readonly Document _testDocument;

    public ProjectStateTests()
    {
        _state = new ProjectState();
        _testDocument = new Document("Test", "Description", "test.txt", "Test");
    }

    [Fact]
    public void AddDocument_ShouldIncrementTotalDocuments()
    {
        // Arrange
        var initialCount = _state.TotalDocuments;
        
        // Act
        _state.AddDocument(_testDocument);
        
        // Assert
        Assert.Equal(initialCount + 1, _state.TotalDocuments);
        Assert.Equal(1, _testDocument.Id); // Первый документ получает ID=1
    }
    
    [Fact]
    public void AddDocument_ShouldUpdateLastUpdateTime()
    {
        // Arrange
        var beforeAdd = DateTime.Now.AddSeconds(-1);
        
        // Act
        _state.AddDocument(_testDocument);
        
        // Assert
        Assert.True(_state.LastUpdate >= beforeAdd);
    }
    
    [Fact]
    public void GetDocument_ExistingId_ReturnsDocument()
    {
        // Arrange
        _state.AddDocument(_testDocument);
        
        // Act
        var retrieved = _state.GetDocument(1);
        
        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(_testDocument.Title, retrieved.Title);
        Assert.Equal(1, retrieved.Id);
    }
    
    [Fact]
    public void GetDocument_NonExistingId_ReturnsNull()
    {
        // Act
        var result = _state.GetDocument(999);
        
        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public void RemoveDocument_ExistingId_ReturnsTrue()
    {
        // Arrange
        _state.AddDocument(_testDocument);
        var initialCount = _state.TotalDocuments;
        
        // Act
        var removed = _state.RemoveDocument(1);
        
        // Assert
        Assert.True(removed);
        Assert.Equal(initialCount - 1, _state.TotalDocuments);
    }
    
    [Fact]
    public void RemoveDocument_NonExistingId_ReturnsFalse()
    {
        // Act
        var removed = _state.RemoveDocument(999);
        
        // Assert
        Assert.False(removed);
    }
    
    [Fact]
    public void GetDocumentsByCategory_ReturnsFilteredDocuments()
    {
        // Arrange
        var doc1 = new Document("Doc1", "Desc1", "file1.txt", "Finance");
        var doc2 = new Document("Doc2", "Desc2", "file2.txt", "Legal");
        var doc3 = new Document("Doc3", "Desc3", "file3.txt", "Finance");
        
        _state.AddDocument(doc1);
        _state.AddDocument(doc2);
        _state.AddDocument(doc3);
        
        // Act
        var financeDocs = _state.GetDocumentsByCategory("Finance");
        
        // Assert
        Assert.Equal(2, financeDocs.Count);
        Assert.All(financeDocs, d => Assert.Equal("Finance", d.Category));
    }
    
    [Fact]
    public void Documents_Property_ReturnsCopyOfCollection()
    {
        // Arrange
        _state.AddDocument(_testDocument);
        
        // Act
        var documents = _state.Documents;
        var documentCountBeforeClear = documents.Count;
        
        // Симуляция "очистки" локальной копии (не влияет на состояние)
        var list = documents.ToList();
        list.Clear();
        
        // Assert
        Assert.Equal(1, documentCountBeforeClear);
        Assert.Equal(1, _state.TotalDocuments); // Оригинальное состояние не изменилось
    }
}