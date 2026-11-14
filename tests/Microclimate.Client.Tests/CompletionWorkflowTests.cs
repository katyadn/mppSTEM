using System.Net;
using System.Text.Json;
using Microclimate.Client.Core.Services;
using Microclimate.Client.Core.ViewModels;
using Microclimate.Contracts.Analytics;
using Microclimate.Contracts.Common;
using Microclimate.Contracts.Rooms;
using Microclimate.Contracts.Thresholds;

namespace Microclimate.Client.Tests;

public sealed class CompletionWorkflowTests
{
    [Theory]
    [InlineData("", "", "", "")]
    [InlineData("NaN", "25", "16", "28")]
    [InlineData("20", "Infinity", "16", "28")]
    [InlineData("26", "25", "16", "28")]
    [InlineData("20", "20", "16", "28")]
    [InlineData("", "", "28", "28")]
    [InlineData("30", "", "", "28")]
    [InlineData("20", "25", "21", "28")]
    public void InvalidThresholdsAreRejectedBeforeHttp(string min, string max, string criticalMin, string criticalMax) =>
        Assert.Throws<ArgumentException>(() => ThresholdsViewModel.Parse(min, max, criticalMin, criticalMax));

    [Fact]
    public void ThresholdInputAcceptsDecimalCommaAndMissingBounds()
    {
        var value = ThresholdsViewModel.Parse("", "25,5", "", "28");
        Assert.Null(value.CriticalMin); Assert.Null(value.WarningMin);
        Assert.Equal(25.5, value.WarningMax); Assert.Equal(28, value.CriticalMax);
    }

    [Theory]
    [InlineData(UserRole.Viewer, false)]
    [InlineData(UserRole.Operator, true)]
    [InlineData(UserRole.Admin, true)]
    public async Task ThresholdEditorChecksRoleAndSendsSelectedMetric(UserRole role, bool allowed)
    {
        using var f = new ClientFixture { Role = role }; await f.SignInAsync();
        var vm = new ThresholdsViewModel(f.Api, f.Session, f.Navigation);
        await vm.LoadAsync(ClientFixture.RoomId, MetricType.Temperature);
        Assert.True(vm.IsLoaded); Assert.Equal(allowed, vm.CanSave);
        vm.WarningMax = "24,5";
        f.Reply = request => request.Method == "PUT" ? ClientFixture.Json(new ThresholdResponse(Guid.NewGuid(), ClientFixture.RoomId, MetricType.Temperature, 20, 24.5, 16, 28)) : f.Respond(request);
        await vm.SaveCommand.ExecuteAsync(null);
        var writes = f.Requests.Where(request => request.Method == "PUT").ToArray();
        Assert.Equal(allowed ? 1 : 0, writes.Length);
        if (allowed)
        {
            Assert.EndsWith("/thresholds/temperature", writes[0].Uri.AbsolutePath, StringComparison.Ordinal);
            Assert.Equal(24.5, JsonDocument.Parse(writes[0].Body!).RootElement.GetProperty("warningMax").GetDouble());
            Assert.Equal("Пороги збережено.", vm.Message);
        }
        else Assert.True(vm.HasError);
    }

    [Fact]
    public async Task DeniedThresholdSavePreservesSessionAndDraft()
    {
        using var f = new ClientFixture(); await f.SignInAsync();
        var vm = new ThresholdsViewModel(f.Api, f.Session, f.Navigation);
        await vm.LoadAsync(ClientFixture.RoomId, MetricType.Temperature); vm.WarningMax = "24";
        f.Reply = _ => new(HttpStatusCode.Forbidden);
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.True(vm.HasError); Assert.NotNull(f.Session.Current); Assert.Equal("24", vm.WarningMax); Assert.Empty(vm.Message);
    }

    [Fact]
    public async Task DashboardUsesLatestValuesServerComfortAndPreservesLoadedPages()
    {
        using var f = new ClientFixture(); await f.SignInAsync();
        f.Reply = request => request.Uri.AbsolutePath.EndsWith("/latest", StringComparison.Ordinal)
            ? ClientFixture.Json(new LatestMetricResponse[] { new(MetricType.Temperature, 0, f.Clock.Now) })
            : request.Uri.AbsolutePath.EndsWith("/comfort", StringComparison.Ordinal)
                ? ClientFixture.Json(new ComfortIndexResponse(0, ComfortCategory.Uncomfortable, [])) : f.Respond(request);
        var vm = new DashboardViewModel(f.Api, f.Navigation);
        await vm.InitializeAsync(); await vm.LoadMoreCommand.ExecuteAsync(null);
        Assert.Equal(2, vm.Rooms.Count);
        await vm.RefreshLiveAsync();
        Assert.Equal(2, vm.Rooms.Count);
        Assert.All(vm.Rooms, card => { Assert.Equal("Некомфортно", card.StatusText); Assert.Contains("0 °C", card.MetricsText, StringComparison.Ordinal); });
    }

    [Fact]
    public async Task ChangingPeriodAutomaticallyRefreshesWithoutOverwritingThresholdDraft()
    {
        using var f = new ClientFixture(); await f.SignInAsync();
        var vm = new RoomDetailsViewModel(f.Api, f.Navigation, f.Clock, f.Session);
        vm.SelectRoom(ClientFixture.RoomId, "101"); await vm.InitializeAsync();
        vm.Thresholds.WarningMax = "24";
        vm.SelectedPeriod = vm.Periods[1]; await vm.SelectionRefresh;
        Assert.Contains(f.Requests, request => request.Uri.Query.Contains("interval=day", StringComparison.Ordinal));
        Assert.Equal("24", vm.Thresholds.WarningMax);
    }

    [Fact]
    public async Task ThemeIsSavedWithoutSigningOutAndRestoredIntoPicker()
    {
        using var f = new ClientFixture(); await f.SignInAsync();
        var vm = new SettingsViewModel(f.Settings, f.Session, f.Navigation);
        await vm.InitializeAsync(); vm.SelectedTheme = vm.Themes[2]; await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal(ClientTheme.Dark, f.Settings.Theme); Assert.NotNull(f.Session.Current);
        var restored = new SettingsViewModel(f.Settings, f.Session, f.Navigation); await restored.InitializeAsync();
        Assert.Equal(ClientTheme.Dark, restored.SelectedTheme.Value);
    }

    [Fact]
    public async Task ThresholdsRoundTripThroughRealBackendAndSqlite()
    {
        await using var factory = new BackendWorkflowTests.SqliteBackendFactory();
        using var http = factory.CreateClient();
        var settings = new MemorySettings { ApiAddress = http.BaseAddress!.AbsoluteUri };
        var session = new ClientSession(new MemoryStore(), TimeProvider.System);
        var api = new ClientApi(http, settings, session, TimeProvider.System);
        await api.LoginAsync("operator@microclimate.local", "OperatorPassword123!", CancellationToken.None);
        var room = Assert.Single((await api.GetRoomsAsync(1, CancellationToken.None)).Items);
        var thresholds = new ThresholdsViewModel(api, session, new TestNavigation());
        await thresholds.LoadAsync(room.Id, MetricType.Temperature);
        Assert.True(thresholds.IsLoaded, thresholds.ErrorMessage);
        thresholds.WarningMax = "24,5"; await thresholds.SaveCommand.ExecuteAsync(null);
        Assert.False(thresholds.HasError, thresholds.ErrorMessage);
        var persisted = (await api.GetThresholdsAsync(room.Id, CancellationToken.None)).Single(item => item.Metric == MetricType.Temperature);
        Assert.Equal(24.5, persisted.WarningMax);
    }
}
