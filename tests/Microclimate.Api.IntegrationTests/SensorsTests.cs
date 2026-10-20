using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Microclimate.Contracts.Auth;
using Microclimate.Contracts.Common;
using Microclimate.Contracts.Rooms;
using Microclimate.Contracts.Sensors;

using Microsoft.AspNetCore.Mvc.Testing;

namespace Microclimate.Api.IntegrationTests;

public sealed class SensorsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
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
    public async Task ListSensorsByRoom_Anonymous_Returns401()
    {
        var response = await _client.GetAsync($"/api/v1/rooms/{Guid.NewGuid()}/sensors");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListSensorsByRoom_AsOperator_ReturnsSensorsList()
    {
        var token = await GetOperatorTokenAsync();

        // Отримуємо кімнату
        using var roomsReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/rooms");
        roomsReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var roomsRes = await _client.SendAsync(roomsReq);
        var paged = await roomsRes.Content.ReadFromJsonAsync<PagedResponse<RoomResponse>>();
        var roomId = paged!.Items[0].Id;

        // Запитуємо датчики кімнати
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/rooms/{roomId}/sensors");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var sensors = await res.Content.ReadFromJsonAsync<List<SensorResponse>>();
        Assert.NotNull(sensors);
        Assert.NotEmpty(sensors);
    }

    [Fact]
    public async Task CreateSensor_AsAdmin_Returns201AndDeviceKey()
    {
        var token = await GetAdminTokenAsync();

        // Створюємо нову кімнату для датчика
        var createRoomReq = new CreateRoomRequest(Guid.NewGuid(), "Серверна-B", 1, 25.0, "ServerRoom");
        using var roomHttpReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/rooms")
        {
            Content = JsonContent.Create(createRoomReq),
        };
        roomHttpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var roomHttpRes = await _client.SendAsync(roomHttpReq);
        var room = await roomHttpRes.Content.ReadFromJsonAsync<RoomResponse>();

        // Створюємо датчик
        var createSensorReq = new CreateSensorRequest(
            room!.Id,
            $"SN-SRV-{Guid.NewGuid().ToString()[..8]}",
            "BME680",
            [MetricType.Temperature, MetricType.Humidity, MetricType.Co2]);

        using var sensorHttpReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/sensors")
        {
            Content = JsonContent.Create(createSensorReq),
        };
        sensorHttpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var sensorRes = await _client.SendAsync(sensorHttpReq);
        Assert.Equal(HttpStatusCode.Created, sensorRes.StatusCode);

        var created = await sensorRes.Content.ReadFromJsonAsync<SensorCreatedResponse>();
        Assert.NotNull(created);
        Assert.NotNull(created.Sensor);
        Assert.Equal(room.Id, created.Sensor.RoomId);
        Assert.False(string.IsNullOrWhiteSpace(created.DeviceKey));
        Assert.Equal(64, created.DeviceKey.Length); // 32 bytes hex
    }

    [Fact]
    public async Task CreateSensor_AsOperator_Returns403Forbidden()
    {
        var token = await GetOperatorTokenAsync();
        var createSensorReq = new CreateSensorRequest(
            Guid.NewGuid(),
            "SN-TEST-403",
            "DHT22",
            [MetricType.Temperature]);

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/sensors")
        {
            Content = JsonContent.Create(createSensorReq),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RotateKey_AsAdmin_ReturnsNewDeviceKey()
    {
        var token = await GetAdminTokenAsync();

        // Знаходимо існуючий датчик
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

        // Ротуємо ключ
        using var rotateReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/sensors/{sensorId}/rotate-key");
        rotateReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var rotateRes = await _client.SendAsync(rotateReq);
        Assert.Equal(HttpStatusCode.OK, rotateRes.StatusCode);

        var result = await rotateRes.Content.ReadFromJsonAsync<SensorCreatedResponse>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.DeviceKey));
        Assert.Equal(64, result.DeviceKey.Length);
    }
}
