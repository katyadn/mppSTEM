using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Microclimate.Contracts.Analytics;
using Microclimate.Contracts.Auth;
using Microclimate.Contracts.Common;
using Microclimate.Contracts.Rooms;

using Microsoft.AspNetCore.Mvc.Testing;

namespace Microclimate.Api.IntegrationTests;

public sealed class AnalyticsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<string> GetOperatorTokenAsync()
    {
        var loginReq = new LoginRequest("operator@microclimate.local", "OperatorPassword123!");
        var res = await _client.PostAsJsonAsync("/api/v1/auth/login", loginReq);
        var tokens = await res.Content.ReadFromJsonAsync<TokenResponse>();
        return tokens!.AccessToken;
    }

    private async Task<Guid> GetSeededRoomIdAsync()
    {
        var token = await GetOperatorTokenAsync();
        using var roomsReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/rooms");
        roomsReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var roomsRes = await _client.SendAsync(roomsReq);
        var paged = await roomsRes.Content.ReadFromJsonAsync<PagedResponse<RoomResponse>>();
        return paged!.Items[0].Id;
    }

    [Fact]
    public async Task GetSummary_ReturnsOk()
    {
        var roomId = await GetSeededRoomIdAsync();
        var token = await GetOperatorTokenAsync();

        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/rooms/{roomId}/analytics/summary?metric=Temperature");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var list = await response.Content.ReadFromJsonAsync<List<MetricSummaryResponse>>();
        Assert.NotNull(list);
    }

    [Fact]
    public async Task GetSeries_ReturnsOk()
    {
        var roomId = await GetSeededRoomIdAsync();
        var token = await GetOperatorTokenAsync();

        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/rooms/{roomId}/analytics/series?metric=Temperature&interval=Hour");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var list = await response.Content.ReadFromJsonAsync<List<SeriesPointResponse>>();
        Assert.NotNull(list);
    }

    [Fact]
    public async Task GetComfortIndex_ReturnsComfortCategory()
    {
        var roomId = await GetSeededRoomIdAsync();
        var token = await GetOperatorTokenAsync();

        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/rooms/{roomId}/analytics/comfort");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var comfort = await response.Content.ReadFromJsonAsync<ComfortIndexResponse>();
        Assert.NotNull(comfort);
    }

    [Fact]
    public async Task GetSummary_ExceedingMaxPeriod90Days_Returns400()
    {
        var roomId = await GetSeededRoomIdAsync();
        var token = await GetOperatorTokenAsync();

        var from = DateTimeOffset.UtcNow.AddDays(-100).ToString("O");
        var to = DateTimeOffset.UtcNow.ToString("O");

        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/rooms/{roomId}/analytics/summary?from={from}&to={to}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
