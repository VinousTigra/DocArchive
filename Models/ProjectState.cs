namespace DocArhive.Models;

using System.Collections.Concurrent;

public class ProjectState
{
    private readonly ConcurrentDictionary<int, Document> _documents = new();
    private int _nextId = 1;
    
    public List<Document> Documents => [.. _documents.Values];
    public int TotalDocuments => _documents.Count;
    public DateTime LastUpdate { get; private set; } = DateTime.Now;
    
    public void AddDocument(Document document)
    {
        document.Id = _nextId++;
        _documents[document.Id] = document;
        LastUpdate = DateTime.Now;
    }
    
    public Document? GetDocument(int id)
    {
        _documents.TryGetValue(id, out var document);
        return document;
    }
    
    public bool RemoveDocument(int id)
    {
        var removed = _documents.TryRemove(id, out _);
        if (removed) LastUpdate = DateTime.Now;
        return removed;
    }
    
    public List<Document> GetDocumentsByCategory(string category)
    {
        return _documents.Values
            .Where(d => d.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}