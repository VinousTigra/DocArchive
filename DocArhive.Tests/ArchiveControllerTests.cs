using DocArhive.Models;

namespace DocArhive.Tests.Controllers;

using DocArhive.Controllers;
using Xunit;


public class ArchiveControllerTests
{
    // Тест 1: Успешное добавление документа
    [Fact]
    public void AddDocument_WithValidData_ShouldAddDocument()
    {
        // Arrange - подготавливаем данные
        var state = new ProjectState(); // реальное хранилище
        var controller = new ArchiveController(state);
        
        var formData = new Dictionary<string, string>
        {
            ["title"] = "Мой документ",
            ["description"] = "Это описание",
            ["filename"] = "document.pdf",
            ["category"] = "Работа"
        };
        
        // Act - выполняем действие
        var result = controller.AddDocument(formData);
        
        // Assert - проверяем результат
        // 1. Проверяем, что документ добавился
        Assert.Equal(1, state.TotalDocuments);
        
        // 2. Проверяем, что в ответе есть текст "успешно"
        Assert.Contains("успешно", result);
        
        // 3. Проверяем, что в ответе есть название документа
        Assert.Contains("Мой документ", result);
    }
    
    // Тест 2: Добавление документа без названия (должно быть ошибка)
    [Fact]
    public void AddDocument_WithoutTitle_ShouldReturnError()
    {
        // Arrange
        var state = new ProjectState();
        var controller = new ArchiveController(state);
        
        var formData = new Dictionary<string, string>
        {
            // Нет title - обязательное поле
            ["filename"] = "document.pdf"
        };
        
        // Act
        var result = controller.AddDocument(formData);
        
        // Assert
        // 1. Проверяем, что документ НЕ добавился
        Assert.Equal(0, state.TotalDocuments);
        
        // 2. Проверяем, что в ответе есть слово "Ошибка"
        Assert.Contains("Ошибка", result);
        
        // 3. Проверяем, что в ответе есть текст про обязательные поля
        Assert.Contains("обязательны", result);
    }
    
    // Тест 3: Добавление документа без имени файла
    [Fact]
    public void AddDocument_WithoutFilename_ShouldReturnError()
    {
        // Arrange
        var state = new ProjectState();
        var controller = new ArchiveController(state);
        
        var formData = new Dictionary<string, string>
        {
            ["title"] = "Мой документ"
            // Нет filename - обязательное поле
        };
        
        // Act
        var result = controller.AddDocument(formData);
        
        // Assert
        Assert.Equal(0, state.TotalDocuments);
        Assert.Contains("Ошибка", result);
        Assert.Contains("обязательны", result);
    }
    
    // Тест 4: Добавление документа только с обязательными полями
    [Fact]
    public void AddDocument_WithOnlyRequiredFields_ShouldWork()
    {
        // Arrange
        var state = new ProjectState();
        var controller = new ArchiveController(state);
    
        var formData = new Dictionary<string, string>
        {
            ["title"] = "Минимальный документ",
            ["filename"] = "file.txt"
            // Нет description и category
        };
    
        // Act
        var result = controller.AddDocument(formData);
    
        // Assert
        // 1. Документ должен добавиться
        Assert.Equal(1, state.TotalDocuments);
    
        // 2. Должен быть успешный ответ
        Assert.Contains("успешно", result);
    
        // 3. Получаем добавленный документ (первый и единственный)
        var addedDoc = state.Documents.FirstOrDefault();
        Assert.NotNull(addedDoc); // Теперь должно работать
    
        // 4. Проверяем, что для description и category установлены значения по умолчанию
        Assert.Equal("", addedDoc.Description); // Пустая строка
        Assert.Equal("Без категории", addedDoc.Category); // Значение по умолчанию
    
        // 5. Проверяем, что title и filename установлены правильно
        Assert.Equal("Минимальный документ", addedDoc.Title);
        Assert.Equal("file.txt", addedDoc.FileName);
    }
    
    // Тест 5: Добавление нескольких документов
    [Fact]
    public void AddDocument_MultipleDocuments_ShouldHaveDifferentIds()
    {
        // Arrange
        var state = new ProjectState();
        var controller = new ArchiveController(state);
    
        // Act - добавляем три документа
        controller.AddDocument(new Dictionary<string, string>
        {
            ["title"] = "Документ 1",
            ["filename"] = "doc1.txt"
        });
    
        controller.AddDocument(new Dictionary<string, string>
        {
            ["title"] = "Документ 2", 
            ["filename"] = "doc2.txt"
        });
    
        controller.AddDocument(new Dictionary<string, string>
        {
            ["title"] = "Документ 3",
            ["filename"] = "doc3.txt"
        });
    
        // Assert
        // Должно быть 3 документа
        Assert.Equal(3, state.TotalDocuments);
    
        // Получаем все документы и сортируем по ID
        var docs = state.Documents.OrderBy(d => d.Id).ToList();
    
        // Теперь проверяем не конкретные ID, а что они последовательные
        // Первый документ может иметь ID не 1, если где-то был добавлен другой
        Assert.Equal(docs[0].Id + 1, docs[1].Id);
        Assert.Equal(docs[1].Id + 1, docs[2].Id);
    
        // Проверяем, что все ID разные
        var uniqueIds = docs.Select(d => d.Id).Distinct().Count();
        Assert.Equal(3, uniqueIds);
    
        // Проверяем названия (они должны быть в наших документах)
        var titles = docs.Select(d => d.Title).ToList();
        Assert.Contains("Документ 1", titles);
        Assert.Contains("Документ 2", titles);
        Assert.Contains("Документ 3", titles);
    }
    
    // Тест 6: Проверка, что ответ содержит JavaScript для перенаправления
    [Fact]
    public void AddDocument_SuccessResponse_ShouldContainRedirectJavaScript()
    {
        // Arrange
        var state = new ProjectState();
        var controller = new ArchiveController(state);
        
        var formData = new Dictionary<string, string>
        {
            ["title"] = "Тест",
            ["filename"] = "test.pdf"
        };
        
        // Act
        var result = controller.AddDocument(formData);
        
        // Assert
        // Проверяем, что в ответе есть JavaScript код
        Assert.Contains("<script>", result);
        Assert.Contains("alert", result);
        Assert.Contains("window.location.href", result);
    }
    
    // Тест 7: Добавление документа с кавычками в названии
    [Fact]
    public void AddDocument_WithQuotesInTitle_ShouldEscapeThem()
    {
        // Arrange
        var state = new ProjectState();
        var controller = new ArchiveController(state);
        
        var formData = new Dictionary<string, string>
        {
            ["title"] = "Документ с 'кавычками'",
            ["filename"] = "test.pdf"
        };
        
        // Act
        var result = controller.AddDocument(formData);
        
        // Assert
        // Проверяем, что кавычки экранированы в JavaScript
        // Вместо ' должно быть \'
        Assert.Contains("Документ с \\'кавычками\\'", result);
    }
}