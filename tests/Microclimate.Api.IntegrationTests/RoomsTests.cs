using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Microclimate.Contracts.Auth;
using Microclimate.Contracts.Common;
using Microclimate.Contracts.Rooms;

using Microsoft.AspNetCore.Mvc.Testing;

namespace Microclimate.Api.IntegrationTests;

public sealed class RoomsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
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
    public async Task ListRooms_Anonymous_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/rooms");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListRooms_Authenticated_ReturnsPagedResult()
    {
        var token = await GetOperatorTokenAsync();
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/rooms");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var paged = await response.Content.ReadFromJsonAsync<PagedResponse<RoomResponse>>();
        Assert.NotNull(paged);
        Assert.NotEmpty(paged.Items);
        Assert.True(paged.TotalCount >= 1);
    }

    [Fact]
    public async Task GetRoomById_NotFound_Returns404()
    {
        var token = await GetOperatorTokenAsync();
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/rooms/{Guid.NewGuid()}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateRoom_AsAdmin_Returns201Created()
    {
        var token = await GetAdminTokenAsync();
        var createReq = new CreateRoomRequest(Guid.NewGuid(), "Аудиторія 205", 2, 60.0, "Classroom");

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/rooms")
        {
            Content = JsonContent.Create(createReq),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<RoomResponse>();
        Assert.NotNull(created);
        Assert.Equal("Аудиторія 205", created.Name);
        Assert.Equal(2, created.Floor);
    }

    [Fact]
    public async Task CreateRoom_AsOperator_Returns403Forbidden()
    {
        var token = await GetOperatorTokenAsync();
        var createReq = new CreateRoomRequest(Guid.NewGuid(), "Аудиторія 999", 3, 50.0, "Classroom");

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/rooms")
        {
            Content = JsonContent.Create(createReq),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateRoom_InvalidFloor_Returns400ValidationProblem()
    {
        var token = await GetAdminTokenAsync();
        var createReq = new CreateRoomRequest(Guid.NewGuid(), "Підземелля", -999, 10.0, "Other");

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/rooms")
        {
            Content = JsonContent.Create(createReq),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteRoom_WithSensors_Returns409Conflict()
    {
        var token = await GetAdminTokenAsync();

        // Отримуємо початкову кімнату, в якій є датчик
        using var listReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/rooms");
        listReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var listRes = await _client.SendAsync(listReq);
        var paged = await listRes.Content.ReadFromJsonAsync<PagedResponse<RoomResponse>>();
        var roomIdWithSensor = paged!.Items[0].Id;

        using var delReq = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/rooms/{roomIdWithSensor}");
        delReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var delRes = await _client.SendAsync(delReq);
        Assert.Equal(HttpStatusCode.Conflict, delRes.StatusCode);
    }
}
