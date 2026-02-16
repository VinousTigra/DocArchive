#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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
        int newId = Interlocked.Increment(ref _nextId);
        document.Id = newId;
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

public class Document
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string FileName { get; set; }
    public DateTime UploadDate { get; set; }
    public string Category { get; set; }
    
    public Document(string title, string? description, string fileName, string? category)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be null, empty or whitespace.", nameof(title));
            
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("FileName cannot be null, empty or whitespace.", nameof(fileName));
        
        Title = title;
        Description = description ?? string.Empty; 
        FileName = fileName;
        Category = category ?? "Без категории"; 
        UploadDate = DateTime.Now;
    }
}