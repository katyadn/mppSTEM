using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Microclimate.Contracts.Auth;
using Microclimate.Contracts.Common;
using Microclimate.Contracts.Measurements;
using Microclimate.Contracts.Rooms;
using Microclimate.Contracts.Sensors;

using Microsoft.AspNetCore.Mvc.Testing;

namespace Microclimate.Api.IntegrationTests;

public sealed class MeasurementsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<string> GetAdminTokenAsync()
    {
        var loginReq = new LoginRequest("admin@microclimate.local", "AdminPassword123!");
        var res = await _client.PostAsJsonAsync("/api/v1/auth/login", loginReq);
        var tokens = await res.Content.ReadFromJsonAsync<TokenResponse>();
        return tokens!.AccessToken;
    }

    private async Task<(Guid RoomId, Guid SensorId)> GetSeededRoomAndSensorAsync()
    {
        var token = await GetAdminTokenAsync();
        using var roomsReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/rooms");
        roomsReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var roomsRes = await _client.SendAsync(roomsReq);
        var paged = await roomsRes.Content.ReadFromJsonAsync<PagedResponse<RoomResponse>>();
        var roomId = paged!.Items[0].Id;

        using var sensorsReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/rooms/{roomId}/sensors");
        sensorsReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var sensorsRes = await _client.SendAsync(sensorsReq);
        var sensors = await sensorsRes.Content.ReadFromJsonAsync<List<SensorResponse>>();
        var sensorId = sensors![0].Id;

        return (roomId, sensorId);
    }

    [Fact]
    public async Task Ingest_WithValidDeviceKey_Returns202Accepted()
    {
        var (roomId, sensorId) = await GetSeededRoomAndSensorAsync();

        var batchReq = new MeasurementBatchRequest(
            sensorId,
            [
                new MeasurementItem(MetricType.Temperature, 22.5, DateTimeOffset.UtcNow),
                new MeasurementItem(MetricType.Humidity, 45.0, DateTimeOffset.UtcNow),
            ]);

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/measurements")
        {
            Content = JsonContent.Create(batchReq),
        };
        req.Headers.Add("X-Device-Key", "test-device-key-12345");

        var response = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task Ingest_WithInvalidDeviceKey_Returns401Unauthorized()
    {
        var (_, sensorId) = await GetSeededRoomAndSensorAsync();

        var batchReq = new MeasurementBatchRequest(
            sensorId,
            [
                new MeasurementItem(MetricType.Temperature, 22.5, DateTimeOffset.UtcNow),
            ]);

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/measurements")
        {
            Content = JsonContent.Create(batchReq),
        };
        req.Headers.Add("X-Device-Key", "invalid-device-key");

        var response = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Ingest_ExceedingPhysicalLimit_Returns400BadRequest()
    {
        var (_, sensorId) = await GetSeededRoomAndSensorAsync();

        // 999 °C перевищує фізичні межі температури [-40; 85]
        var batchReq = new MeasurementBatchRequest(
            sensorId,
            [
                new MeasurementItem(MetricType.Temperature, 999.0, DateTimeOffset.UtcNow),
            ]);

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/measurements")
        {
            Content = JsonContent.Create(batchReq),
        };
        req.Headers.Add("X-Device-Key", "test-device-key-12345");

        var response = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMeasurementsByRoom_ReturnsOkWithPagedResult()
    {
        var (roomId, _) = await GetSeededRoomAndSensorAsync();
        var token = await GetAdminTokenAsync();

        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/rooms/{roomId}/measurements?metric=Temperature");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var paged = await response.Content.ReadFromJsonAsync<PagedResponse<MeasurementResponse>>();
        Assert.NotNull(paged);
    }

    [Fact]
    public async Task ExportCsv_ReturnsCsvContentType()
    {
        var (roomId, _) = await GetSeededRoomAndSensorAsync();
        var token = await GetAdminTokenAsync();

        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/rooms/{roomId}/measurements/export");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);

        var csv = await response.Content.ReadAsStringAsync();
        Assert.Contains("MeasuredAt,SensorId,Metric,Value", csv);
    }
}
