using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Microclimate.Contracts.Alerts;
using Microclimate.Contracts.Auth;
using Microclimate.Contracts.Common;
using Microclimate.Contracts.Measurements;
using Microclimate.Contracts.Rooms;
using Microclimate.Contracts.Sensors;
using Microclimate.Contracts.Thresholds;

using Microsoft.AspNetCore.Mvc.Testing;

namespace Microclimate.Api.IntegrationTests;

public sealed class ThresholdsAndAlertsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
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
    public async Task GetThresholds_ReturnsAllMetricsDefaults()
    {
        var (roomId, _) = await GetSeededRoomAndSensorAsync();
        var token = await GetOperatorTokenAsync();

        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/rooms/{roomId}/thresholds");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var list = await response.Content.ReadFromJsonAsync<List<ThresholdResponse>>();
        Assert.NotNull(list);
        Assert.NotEmpty(list);
        Assert.Contains(list, t => t.Metric == MetricType.Temperature);
    }

    [Fact]
    public async Task UpdateThreshold_AsOperator_ReturnsUpdated()
    {
        var (roomId, _) = await GetSeededRoomAndSensorAsync();
        var token = await GetOperatorTokenAsync();

        var updateReq = new UpdateThresholdRequest(
            WarningMin: 18.0,
            WarningMax: 24.0,
            CriticalMin: 15.0,
            CriticalMax: 28.0);

        using var req = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/rooms/{roomId}/thresholds/Temperature")
        {
            Content = JsonContent.Create(updateReq),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<ThresholdResponse>();
        Assert.NotNull(updated);
        Assert.Equal(18.0, updated.WarningMin);
        Assert.Equal(24.0, updated.WarningMax);
    }

    [Fact]
    public async Task UpdateThreshold_InvalidLimits_Returns400()
    {
        var (roomId, _) = await GetSeededRoomAndSensorAsync();
        var token = await GetOperatorTokenAsync();

        // CriticalMin (30) > WarningMin (20) — невалідно
        var updateReq = new UpdateThresholdRequest(
            WarningMin: 20.0,
            WarningMax: 25.0,
            CriticalMin: 30.0,
            CriticalMax: 35.0);

        using var req = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/rooms/{roomId}/thresholds/Temperature")
        {
            Content = JsonContent.Create(updateReq),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Ingestion_ExceedingThreshold_CreatesAlert_AndCanBeAcknowledged()
    {
        var (roomId, sensorId) = await GetSeededRoomAndSensorAsync();
        var operatorToken = await GetOperatorTokenAsync();

        // 35.0 °C для офісу перевищує критичну межу 28 °C
        var batchReq = new MeasurementBatchRequest(
            sensorId,
            [
                new MeasurementItem(MetricType.Temperature, 35.0, DateTimeOffset.UtcNow),
            ]);

        using var ingestHttpReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/measurements")
        {
            Content = JsonContent.Create(batchReq),
        };
        ingestHttpReq.Headers.Add("X-Device-Key", "test-device-key-12345");

        var ingestRes = await _client.SendAsync(ingestHttpReq);
        Assert.Equal(HttpStatusCode.Accepted, ingestRes.StatusCode);

        // Перевіряємо, що з'явився alert
        using var alertsHttpReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/alerts?roomId={roomId}");
        alertsHttpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", operatorToken);

        var alertsRes = await _client.SendAsync(alertsHttpReq);
        Assert.Equal(HttpStatusCode.OK, alertsRes.StatusCode);

        var pagedAlerts = await alertsRes.Content.ReadFromJsonAsync<PagedResponse<AlertResponse>>();
        Assert.NotNull(pagedAlerts);
        Assert.NotEmpty(pagedAlerts.Items);

        var alert = pagedAlerts.Items[0];
        Assert.Equal(AlertLevel.Critical, alert.Level);
        Assert.Equal(AlertStatus.Open, alert.Status);

        // Квитуємо сповіщення
        using var ackHttpReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/alerts/{alert.Id}/acknowledge");
        ackHttpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", operatorToken);

        var ackRes = await _client.SendAsync(ackHttpReq);
        Assert.Equal(HttpStatusCode.OK, ackRes.StatusCode);

        var acked = await ackRes.Content.ReadFromJsonAsync<AlertResponse>();
        Assert.NotNull(acked);
        Assert.Equal(AlertStatus.Acknowledged, acked.Status);
        Assert.NotNull(acked.AcknowledgedAt);
    }
}
