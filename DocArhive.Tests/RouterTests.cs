namespace DocArhive.Tests.Services;

using DocArhive.Models;
using DocArhive;
using DocArhive.Controllers;
using FluentAssertions;
using Moq;
using Xunit;

public class RouterTests
{
    private readonly Mock<HomeController> _mockHomeController;
    private readonly Mock<ArchiveController> _mockArchiveController;
    private readonly Router _router;

    public RouterTests()
    {
        var mockProjectState = new Mock<ProjectState>();
        _mockHomeController = new Mock<HomeController>(mockProjectState.Object);
        _mockArchiveController = new Mock<ArchiveController>(mockProjectState.Object);

        // Use reflection to create router with mocked controllers
        _router = new Router(mockProjectState.Object);

        // Replace the controllers with mocks
        var homeControllerField = typeof(Router).GetField("_homeController",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var archiveControllerField = typeof(Router).GetField("_archiveController",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        homeControllerField?.SetValue(_router, _mockHomeController.Object);
        archiveControllerField?.SetValue(_router, _mockArchiveController.Object);
    }

    [Theory]
    [InlineData("GET", "/", "Home Page")]
    [InlineData("GET", "/status", "Status Page")]
    public void Route_ValidGetRequests_ShouldReturnOkResponse(string method, string path, string expectedContent)
    {
        // Arrange
        var request = new HttpRequest
        {
            Method = method,
            Path = path,
            Headers = new Dictionary<string, string>(),
            Body = ""
        };

        _mockHomeController.Setup(x => x.Index()).Returns(expectedContent);
        _mockHomeController.Setup(x => x.Status()).Returns(expectedContent);

        // Act
        var response = _router.Route(request);

        // Assert
        response.StatusCode.Should().Be(200);
        response.StatusMessage.Should().Be("OK");
        response.Content.Should().Be(expectedContent);
    }

    [Fact]
    public void Route_ValidPostRequest_ShouldParseFormDataAndCallController()
    {
        // Arrange
        var request = new HttpRequest
        {
            Method = "POST",
            Path = "/action",
            Headers = new Dictionary<string, string>(),
            Body = "title=Test&description=Desc&filename=test.pdf&category=Test"
        };

        _mockArchiveController.Setup(x => x.AddDocument(It.IsAny<Dictionary<string, string>>()))
            .Returns("Success");

        // Act
        var response = _router.Route(request);

        // Assert
        response.StatusCode.Should().Be(200);
        _mockArchiveController.Verify(x => x.AddDocument(It.Is<Dictionary<string, string>>(d =>
            d["title"] == "Test" &&
            d["description"] == "Desc" &&
            d["filename"] == "test.pdf" &&
            d["category"] == "Test"
        )), Times.Once);
    }

    [Theory]
    [InlineData("PUT", "/")]
    [InlineData("DELETE", "/")]
    [InlineData("PATCH", "/")]
    public void Route_UnsupportedMethod_ShouldReturn405(string method, string path)
    {
        // Arrange
        var request = new HttpRequest
        {
            Method = method,
            Path = path,
            Headers = new Dictionary<string, string>(),
            Body = ""
        };

        // Act
        var response = _router.Route(request);

        // Assert
        response.StatusCode.Should().Be(405);
        response.StatusMessage.Should().Be("Method Not Allowed");
        response.Content.Should().Contain("405");
    }

    [Theory]
    [InlineData("/unknown")]
    [InlineData("/api/test")]
    [InlineData("/docs/index.html")]
    public void Route_UnknownPath_ShouldReturn404(string path)
    {
        // Arrange
        var request = new HttpRequest
        {
            Method = "GET",
            Path = path,
            Headers = new Dictionary<string, string>(),
            Body = ""
        };

        // Act
        var response = _router.Route(request);

        // Assert
        response.StatusCode.Should().Be(404);
        response.StatusMessage.Should().Be("Not Found");
        response.Content.Should().Contain("404");
    }

    [Fact]
    public void ParseFormData_WithEncodedValues_ShouldDecodeProperly()
    {
        // Arrange
        const string body = "title=Test%20Document&filename=report%2Epdf&category=Finance%26Legal";

        // Act - use reflection to test private method
        var method = typeof(Router).GetMethod("ParseFormData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = method?.Invoke(null, new object[] { body }) as Dictionary<string, string>;

        // Assert
        result.Should().NotBeNull();
        result!.Should().ContainKey("title");
        result["title"].Should().Be("Test Document");
        result["filename"].Should().Be("report.pdf");
        result["category"].Should().Be("Finance&Legal");
    }

    [Fact]
    public void Route_WhenExceptionThrown_ShouldReturn500()
    {
        // Arrange
        var request = new HttpRequest
        {
            Method = "GET",
            Path = "/",
            Headers = new Dictionary<string, string>(),
            Body = ""
        };
    
        // Правильный способ создания исключения с сообщением
        _mockHomeController.Setup(x => x.Index())
            .Throws(new InvalidOperationException("Test error"));
    
        // Act
        var response = _router.Route(request);
    
        // Assert
        response.StatusCode.Should().Be(500);
        response.StatusMessage.Should().Be("Internal Server Error");
        response.Content.Should().Contain("500");
        response.Content.Should().Contain("Test error");
    }
}