using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Microclimate.Contracts.Auth;
using Microclimate.Contracts.Common;

using Microsoft.AspNetCore.Mvc.Testing;

namespace Microclimate.Api.IntegrationTests;

/// <summary>
/// Інтеграційні тести автентифікації JWT:
/// логін, валідація, отримання профілю /me та оновлення токенів refresh.
/// </summary>
public sealed class AuthTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        // Arrange
        var request = new LoginRequest("admin@microclimate.local", "AdminPassword123!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(token);
        Assert.False(string.IsNullOrWhiteSpace(token.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(token.RefreshToken));
        Assert.Equal(900, token.ExpiresIn);
        Assert.Equal("Bearer", token.TokenType);
    }

    [Fact]
    public async Task Login_InvalidPassword_Returns401()
    {
        // Arrange
        var request = new LoginRequest("admin@microclimate.local", "WrongPassword123!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Login_InvalidEmail_Returns400ValidationProblem()
    {
        // Arrange
        var request = new LoginRequest("not-an-email", "Short");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithBearerToken_ReturnsUserProfile()
    {
        // Arrange - login first
        var loginReq = new LoginRequest("admin@microclimate.local", "AdminPassword123!");
        var loginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", loginReq);
        var tokens = await loginRes.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(tokens);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(user);
        Assert.Equal("admin@microclimate.local", user.Email);
        Assert.Equal(UserRole.Admin, user.Role);
    }

    [Fact]
    public async Task Refresh_ValidRefreshToken_ReturnsNewTokens()
    {
        // Arrange - login first
        var loginReq = new LoginRequest("operator@microclimate.local", "OperatorPassword123!");
        var loginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", loginReq);
        var tokens = await loginRes.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(tokens);

        var refreshReq = new RefreshTokenRequest(tokens.RefreshToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshReq);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var newTokens = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(newTokens);
        Assert.False(string.IsNullOrWhiteSpace(newTokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(newTokens.RefreshToken));
    }

    [Fact]
    public async Task Refresh_InvalidToken_Returns401()
    {
        // Arrange
        var refreshReq = new RefreshTokenRequest("invalid.fake.token");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshReq);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
