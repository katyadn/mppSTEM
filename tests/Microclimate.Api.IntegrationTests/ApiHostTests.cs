using System.Net;
using System.Net.Http.Json;
using Microclimate.Contracts.Service;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Microclimate.Api.IntegrationTests;

/// <summary>
/// Інтеграційні тести для перевірки базової конфігурації хоста API,
/// версіонування, health-check та стандартизованого повернення помилок ProblemDetails.
/// </summary>
public sealed class ApiHostTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_ReturnsOk()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", content);
    }

    [Fact]
    public async Task Ping_V1_ReturnsOkWithVersion()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/v1/ping");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var ping = await response.Content.ReadFromJsonAsync<PingResponse>();
        Assert.NotNull(ping);
        Assert.Equal("ok", ping.Status);
        Assert.Equal("1.0", ping.Version);
        Assert.True(ping.ServerTime <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Ping_ResponseContainsApiVersionHeader()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/v1/ping");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(
            response.Headers.Contains("api-supported-versions"),
            "Відповідь повинна містити заголовок api-supported-versions");

        var versions = response.Headers.GetValues("api-supported-versions");
        Assert.Contains("1.0", versions);
    }

    [Fact]
    public async Task UnknownRoute_ReturnsProblemDetails404()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/v1/non-existent-route-for-testing");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task UnsupportedVersion_Returns400()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/v99/ping");

        // Assert: непідтримувана версія у сегменті повертає 400 Bad Request або 404
        Assert.True(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound,
            $"Очікувався код помилки версіонування 400 або 404, отримано: {response.StatusCode}");
    }
}
