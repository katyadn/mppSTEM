using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using Microclimate.Contracts.Analytics;
using Microclimate.Contracts.Auth;
using Microclimate.Contracts.Common;
using Microclimate.Contracts.Measurements;
using Microclimate.Contracts.Rooms;
using Microclimate.Contracts.Sensors;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Microclimate.Api.IntegrationTests;

/// <summary>API з реальною БД SQLite: міграції, демо-дані, прийом вимірів і аналітика.</summary>
public sealed class DatabaseFlowTests : IClassFixture<DatabaseFlowTests.SqliteApiFactory>
{
    private readonly HttpClient _client;

    public DatabaseFlowTests(SqliteApiFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task IngestedMeasurement_IsStoredInDatabase_CountedInSummary_AndExported()
    {
        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin@microclimate.local", "AdminPassword123!"));
        var token = (await login.Content.ReadFromJsonAsync<TokenResponse>())!.AccessToken;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var rooms = await _client.GetFromJsonAsync<PagedResponse<RoomResponse>>("/api/v1/rooms");
        var roomId = rooms!.Items[0].Id;
        var sensors = await _client.GetFromJsonAsync<List<SensorResponse>>($"/api/v1/rooms/{roomId}/sensors");

        using var ingest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/measurements")
        {
            Content = JsonContent.Create(new MeasurementBatchRequest(
                sensors![0].Id,
                [new MeasurementItem(MetricType.Temperature, 22.5, DateTimeOffset.UtcNow.AddMinutes(-1))])),
        };
        ingest.Headers.Add("X-Device-Key", "test-device-key-12345");
        var ingestResponse = await _client.SendAsync(ingest);

        var summary = await _client.GetFromJsonAsync<List<MetricSummaryResponse>>(
            $"/api/v1/rooms/{roomId}/analytics/summary?metric=Temperature");
        var export = await _client.GetAsync($"/api/v1/rooms/{roomId}/measurements/export?format=json");
        using var exported = JsonDocument.Parse(await export.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.Accepted, ingestResponse.StatusCode);
        var temperature = Assert.Single(summary!);
        Assert.Equal(1, temperature.Count);
        Assert.Equal(22.5, temperature.Avg);
        Assert.Equal("application/json", export.Content.Headers.ContentType?.MediaType);
        Assert.Contains(exported.RootElement.EnumerateArray(), row => row.GetProperty("value").GetDouble() == 22.5);
    }

    /// <summary>Окремий файл БД на кожен запуск, щоб тести не залежали один від одного.</summary>
    public sealed class SqliteApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"microclimate-test-{Guid.NewGuid():N}.db");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Database:Provider", "Sqlite");
            builder.UseSetting("Database:ConnectionString", $"Data Source={_databasePath};Pooling=False");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            File.Delete(_databasePath);
        }
    }
}
