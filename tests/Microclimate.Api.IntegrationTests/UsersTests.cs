using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Microclimate.Contracts.Auth;
using Microclimate.Contracts.Common;
using Microclimate.Contracts.Users;

using Microsoft.AspNetCore.Mvc.Testing;

namespace Microclimate.Api.IntegrationTests;

public sealed class UsersTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<string> GetAdminTokenAsync()
    {
        var loginReq = new LoginRequest("admin@microclimate.local", "AdminPassword123!");
        var res = await _client.PostAsJsonAsync("/api/v1/auth/login", loginReq);
        var tokens = await res.Content.ReadFromJsonAsync<TokenResponse>();
        return tokens!.AccessToken;
    }

    private async Task<string> GetOperatorTokenAsync()
    {
        var loginReq = new LoginRequest("operator@microclimate.local", "OperatorPassword123!");
        var res = await _client.PostAsJsonAsync("/api/v1/auth/login", loginReq);
        var tokens = await res.Content.ReadFromJsonAsync<TokenResponse>();
        return tokens!.AccessToken;
    }

    [Fact]
    public async Task ListUsers_AsAdmin_ReturnsUsersList()
    {
        var token = await GetAdminTokenAsync();
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var list = await response.Content.ReadFromJsonAsync<List<UserResponse>>();
        Assert.NotNull(list);
        Assert.NotEmpty(list);
    }

    [Fact]
    public async Task ListUsers_AsOperator_Returns403Forbidden()
    {
        var token = await GetOperatorTokenAsync();
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_AsAdmin_Returns201Created()
    {
        var token = await GetAdminTokenAsync();
        var createReq = new CreateUserRequest($"newuser-{Guid.NewGuid().ToString()[..6]}@microclimate.local", "SuperSecurePass123!", UserRole.Viewer);

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users")
        {
            Content = JsonContent.Create(createReq),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(created);
        Assert.Equal(createReq.Email.ToLowerInvariant(), created.Email);
        Assert.Equal(UserRole.Viewer, created.Role);
    }

    [Fact]
    public async Task CreateUser_ShortPassword_Returns400ValidationProblem()
    {
        var token = await GetAdminTokenAsync();
        var createReq = new CreateUserRequest("testshort@microclimate.local", "123", UserRole.Viewer);

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users")
        {
            Content = JsonContent.Create(createReq),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangeRole_AsAdmin_UpdatesRole()
    {
        var token = await GetAdminTokenAsync();

        // Створюємо користувача
        var createReq = new CreateUserRequest($"roleuser-{Guid.NewGuid().ToString()[..6]}@microclimate.local", "StrongPassword123!", UserRole.Viewer);
        using var postReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users")
        {
            Content = JsonContent.Create(createReq),
        };
        postReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var postRes = await _client.SendAsync(postReq);
        var created = await postRes.Content.ReadFromJsonAsync<UserResponse>();

        // Змінюємо роль на Operator
        var changeRoleReq = new ChangeRoleRequest(UserRole.Operator);
        using var putReq = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/users/{created!.Id}/role")
        {
            Content = JsonContent.Create(changeRoleReq),
        };
        putReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var putRes = await _client.SendAsync(putReq);
        Assert.Equal(HttpStatusCode.OK, putRes.StatusCode);

        var updated = await putRes.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(updated);
        Assert.Equal(UserRole.Operator, updated.Role);
    }
}
