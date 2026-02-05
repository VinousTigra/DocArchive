namespace DocArhive.Tests.Integration;

using FluentAssertions;
using Xunit;


public class HttpServerIntegrationTests : IDisposable
{
    private readonly HttpServer _server;
    private readonly Thread _serverThread;
    private readonly string _baseUrl = "http://127.0.0.1:8081";
    
    public HttpServerIntegrationTests()
    {
        _server = new HttpServer("127.0.0.1", 8081);
        _serverThread = new Thread(() => _server.Start());
        _serverThread.Start();
        
        // Wait for server to start
        Thread.Sleep(1000);
    }
    
    public void Dispose()
    {
        _server.Stop();
        _serverThread.Join(1000);
    }
    
    [Fact]
    public async Task Server_ShouldRespondToGetRequest()
    {
        // Act
        using var client = new HttpClient();
        var response = await client.GetAsync($"{_baseUrl}/");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Архив документов");
    }
    
    [Fact]
    public async Task Server_ShouldHandleParallelRequests()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();
        using var client = new HttpClient();
        
        // Act
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(client.GetAsync($"{_baseUrl}/status"));
        }
        
        var responses = await Task.WhenAll(tasks);
        
        // Assert
        responses.Should().AllSatisfy(r => r.EnsureSuccessStatusCode());
    }
    
    [Fact]
    public void Server_OnInvalidPort_ShouldThrowException()
    {
        // Act
        Action act = () => new HttpServer("127.0.0.1", 99999);
        
        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}