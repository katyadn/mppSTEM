using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microclimate.Client.Core.Services;
using Microclimate.Client.Core.ViewModels;
using Microclimate.Contracts.Common;
using Microclimate.Contracts.Measurements;
using Microclimate.Contracts.Sensors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Microclimate.Client.Tests;

public sealed class BackendWorkflowTests
{
    [Fact]
    public async Task ClientUsesCurrentBackendWithSqliteForLoginRoomsAnalyticsAndAcknowledgement()
    {
        await using var factory = new SqliteBackendFactory();
        using var http = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        var settings = new MemorySettings { ApiAddress = http.BaseAddress!.AbsoluteUri };
        var store = new MemoryStore();
        var session = new ClientSession(store, TimeProvider.System);
        var navigation = new TestNavigation();
        var api = new ClientApi(http, settings, session, TimeProvider.System);
        var login = new LoginViewModel(api, navigation) { Email = "admin@microclimate.local", Password = "AdminPassword123!" };
        await login.LoginCommand.ExecuteAsync(null);
        Assert.False(login.HasError, login.ErrorMessage);
        Assert.Equal("dashboard", navigation.Destination);
        Assert.Equal(UserRole.Admin, session.Current!.User.Role);

        var dashboard = new DashboardViewModel(api, navigation);
        await dashboard.InitializeAsync();
        Assert.False(dashboard.HasError, dashboard.ErrorMessage);
        var room = Assert.Single(dashboard.Rooms);
        Assert.Equal("Лабораторія 101", room.Name);
        Assert.Equal("Немає даних", room.StatusText);
        Assert.False(dashboard.HasMore);

        var details = new RoomDetailsViewModel(api, navigation, TimeProvider.System);
        details.SelectRoom(room.Id, room.Name);
        await details.InitializeAsync();
        Assert.False(details.HasError, details.ErrorMessage);
        Assert.Equal("Немає даних", details.SummaryText);
        Assert.False(details.HasSeries);

        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Current.AccessToken);
        var sensors = await http.GetFromJsonAsync<List<SensorResponse>>($"/api/v1/rooms/{room.Id}/sensors");
        var measuredAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        using var ingest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/measurements")
        {
            Content = JsonContent.Create(new MeasurementBatchRequest(Assert.Single(sensors!).Id,
                [new MeasurementItem(MetricType.Temperature, 22.5, measuredAt), new MeasurementItem(MetricType.Co2, 1900, measuredAt)])),
        };
        ingest.Headers.Add("X-Device-Key", "test-device-key-12345");
        using var response = await http.SendAsync(ingest);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        await details.RefreshCommand.ExecuteAsync(null);
        Assert.False(details.HasError, details.ErrorMessage);
        Assert.Contains("22.5", details.SummaryText, StringComparison.Ordinal);
        Assert.True(details.HasSeries);
        Assert.Equal(22.5, Assert.Single(details.Series.SelectMany(segment => segment)).Value);
        Assert.NotEqual("Немає даних", details.ComfortText);
        details.SelectedPeriod = details.Periods[1];
        await details.RefreshCommand.ExecuteAsync(null);
        Assert.False(details.HasError, details.ErrorMessage);
        Assert.True(details.HasSeries);

        var alerts = new AlertsViewModel(api, session, navigation);
        alerts.SelectedLevel = alerts.Levels[2];
        alerts.SelectedStatus = alerts.Statuses[1];
        await alerts.InitializeAsync();
        Assert.False(alerts.HasError, alerts.ErrorMessage);
        var alert = Assert.Single(alerts.Alerts);
        Assert.True(alert.CanAcknowledge);
        await alerts.AcknowledgeCommand.ExecuteAsync(alert);
        Assert.False(alerts.HasError, alerts.ErrorMessage);
        Assert.Empty(alerts.Alerts);
        var acknowledged = await api.GetAlertsAsync(1, AlertLevel.Critical, AlertStatus.Acknowledged, CancellationToken.None);
        Assert.Equal(alert.Alert.Id, Assert.Single(acknowledged.Items).Id);

        var preferences = new SettingsViewModel(settings, session, navigation);
        await preferences.LogoutCommand.ExecuteAsync(null);
        Assert.Null(session.Current);
        Assert.Null(store.Value);
        Assert.Equal("login", navigation.Destination);
    }

    private sealed class SqliteBackendFactory : WebApplicationFactory<Program>
    {
        private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"microclimate-client-{Guid.NewGuid():N}.db");
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Database:Provider", "Sqlite");
            builder.UseSetting("Database:ConnectionString", $"Data Source={databasePath};Pooling=False");
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            File.Delete(databasePath);
        }
    }
}
