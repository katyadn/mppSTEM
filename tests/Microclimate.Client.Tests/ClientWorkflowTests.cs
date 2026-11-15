using System.Net;
using Microclimate.Client.Core.ViewModels;
using Microclimate.Contracts.Common;

namespace Microclimate.Client.Tests;

// Перевірки клієнтських сценаріїв ViewModel без нативного інтерфейсу.
public sealed class ClientWorkflowTests
{
    [Fact]
    public async Task LoginValidationDoesNotSendRequestAndSuccessfulLoginClearsPassword()
    {
        using var f = new ClientFixture();
        var vm = new LoginViewModel(f.Api, f.Navigation) { Email = "invalid", Password = "abc" };
        await vm.LoginCommand.ExecuteAsync(null);
        Assert.True(vm.HasError);
        Assert.Empty(f.Requests);
        vm.Email = "student@example.test";
        await vm.LoginCommand.ExecuteAsync(null);
        Assert.Equal("dashboard", f.Navigation.Destination);
        Assert.Empty(vm.Password);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task FailedLoginStaysOnFormAndShowsMessage()
    {
        using var f = new ClientFixture { Reply = _ => new(HttpStatusCode.Unauthorized) };
        var vm = new LoginViewModel(f.Api, f.Navigation) { Email = "student@example.test", Password = "wrong" };
        await vm.LoginCommand.ExecuteAsync(null);
        Assert.True(vm.HasError);
        Assert.Empty(f.Navigation.Destination);
        Assert.Empty(vm.Password);
    }

    [Fact]
    public async Task DashboardLoadsNextPageAndRefreshReplacesOldItems()
    {
        using var f = new ClientFixture();
        await f.SignInAsync();
        var vm = new DashboardViewModel(f.Api, f.Navigation);
        await vm.InitializeAsync();
        Assert.Single(vm.Rooms);
        Assert.True(vm.HasMore);
        await vm.LoadMoreCommand.ExecuteAsync(null);
        Assert.Equal(2, vm.Rooms.Count);
        Assert.False(vm.HasMore);
        await vm.RefreshCommand.ExecuteAsync(null);
        Assert.Single(vm.Rooms);
        await vm.OpenSelectedRoomCommand.ExecuteAsync(vm.Rooms[0]);
        Assert.Equal(ClientFixture.RoomId, f.Navigation.RoomId);
    }

    [Fact]
    public async Task UnauthorizedDashboardReturnsToLoginButUnavailableEndpointShowsError()
    {
        using var f = new ClientFixture();
        await f.SignInAsync();
        var vm = new DashboardViewModel(f.Api, f.Navigation);
        f.Reply = _ => new(HttpStatusCode.NotFound);
        await vm.InitializeAsync();
        Assert.True(vm.HasError);
        Assert.Empty(f.Navigation.Destination);
        Assert.False(vm.IsBusy);
        f.Reply = _ => new(HttpStatusCode.Unauthorized);
        await vm.RefreshCommand.ExecuteAsync(null);
        Assert.Equal("login", f.Navigation.Destination);
    }

    [Fact]
    public async Task RoomDetailsUseSelectedPeriodAndPreserveChartGap()
    {
        using var f = new ClientFixture();
        await f.SignInAsync();
        var vm = new RoomDetailsViewModel(f.Api, f.Navigation, f.Clock, f.Session);
        vm.SelectRoom(ClientFixture.RoomId, "101");
        vm.SelectedPeriod = vm.Periods[1];
        vm.SelectedMetric = vm.Metrics[2];
        await vm.InitializeAsync();
        Assert.Equal(6, vm.Charts.Count);
        var chart = vm.Charts[2];
        Assert.All(vm.Charts, item => Assert.Equal(2, item.Series.Count));
        Assert.All(vm.Charts, item => Assert.True(item.HasSeries));
        Assert.False(chart.HasNoSeries);
        Assert.Equal("82/100 · Прийнятно", vm.ComfortText);
        Assert.Contains("ppm", chart.SummaryText, StringComparison.Ordinal);
        var seriesRequests = f
            .Requests.Where(request => request.Uri.AbsolutePath.EndsWith("/series", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(6, seriesRequests.Length);
        Assert.All(seriesRequests, request =>
            Assert.Contains("interval=day", request.Uri.Query, StringComparison.Ordinal)
        );
        Assert.Single(
            seriesRequests
                .Select(request =>
                    string.Join(
                        "&",
                        request
                            .Uri.Query.Split('&')
                            .Where(part =>
                                part.StartsWith("from=", StringComparison.Ordinal)
                                || part.StartsWith("to=", StringComparison.Ordinal)
                            )
                    )
                )
                .Distinct()
        );
    }

    [Fact]
    public async Task FailedNewAnalyticsRequestClearsOldChart()
    {
        using var f = new ClientFixture();
        await f.SignInAsync();
        var vm = new RoomDetailsViewModel(f.Api, f.Navigation, f.Clock, f.Session);
        vm.SelectRoom(ClientFixture.RoomId, "101");
        await vm.InitializeAsync();
        Assert.All(vm.Charts, item => Assert.True(item.HasSeries));
        f.Reply = _ => new(HttpStatusCode.InternalServerError);
        await vm.RefreshCommand.ExecuteAsync(null);
        Assert.All(vm.Charts, item => Assert.Empty(item.Series));
        Assert.All(vm.Charts, item => Assert.True(item.HasNoSeries));
        Assert.True(vm.HasError);
    }

    [Theory]
    [InlineData(UserRole.Operator, true)]
    [InlineData(UserRole.Admin, true)]
    [InlineData(UserRole.Viewer, false)]
    public async Task AlertAcknowledgementRespectsRole(UserRole role, bool permitted)
    {
        using var f = new ClientFixture { Role = role };
        await f.SignInAsync();
        var vm = new AlertsViewModel(f.Api, f.Session, f.Navigation);
        await vm.InitializeAsync();
        var item = Assert.Single(vm.Alerts);
        Assert.Equal(permitted, item.CanAcknowledge);
        await vm.AcknowledgeCommand.ExecuteAsync(item);
        Assert.Equal(permitted, f.Acknowledged);
        if (permitted)
            Assert.False(Assert.Single(vm.Alerts).CanAcknowledge);
        else
            Assert.True(vm.HasError);
    }

    [Fact]
    public async Task AlertsPassSelectedFiltersToServer()
    {
        using var f = new ClientFixture();
        await f.SignInAsync();
        var vm = new AlertsViewModel(f.Api, f.Session, f.Navigation);
        vm.SelectedLevel = vm.Levels[2];
        vm.SelectedStatus = vm.Statuses[1];
        await vm.InitializeAsync();
        var request = Assert.Single(f.Requests);
        Assert.Contains("level=critical", request.Uri.Query, StringComparison.Ordinal);
        Assert.Contains("status=open", request.Uri.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ChangingApiAddressClearsSessionAndInvalidAddressDoesNotChangeSettings()
    {
        using var f = new ClientFixture();
        await f.SignInAsync();
        var vm = new SettingsViewModel(f.Settings, f.Session, f.Navigation);
        await vm.InitializeAsync();
        vm.ServerAddress = "http://remote.example.test";
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.True(vm.HasError);
        Assert.NotNull(f.Session.Current);
        Assert.Equal("https://api.example.test/", f.Settings.ApiAddress);
        vm.ServerAddress = "https://new.example.test";
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Null(f.Session.Current);
        Assert.Equal("login", f.Navigation.Destination);
        Assert.Equal("https://new.example.test/", f.Settings.ApiAddress);
    }

    [Fact]
    public async Task LogoutRemovesStoredSession()
    {
        using var f = new ClientFixture();
        await f.SignInAsync();
        var vm = new SettingsViewModel(f.Settings, f.Session, f.Navigation);
        await vm.LogoutCommand.ExecuteAsync(null);
        Assert.Null(f.Store.Value);
        Assert.Null(f.Session.Current);
        Assert.Equal("login", f.Navigation.Destination);
    }

    [Fact]
    public async Task LeavingPageCancelsPendingRequestWithoutShowingError()
    {
        using var f = new ClientFixture();
        await f.SignInAsync();
        f.DelayRooms = true;
        var vm = new DashboardViewModel(f.Api, f.Navigation);
        var load = vm.InitializeAsync();
        await f.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        vm.CancelPending();
        await load;
        Assert.False(vm.IsBusy);
        Assert.False(vm.HasError);
        Assert.Empty(vm.Rooms);
    }

    [Fact]
    public async Task FailedRefreshDoesNotExposePreviousAccountRooms()
    {
        using var f = new ClientFixture();
        await f.SignInAsync();
        var vm = new DashboardViewModel(f.Api, f.Navigation);
        await vm.InitializeAsync();
        Assert.Single(vm.Rooms);
        f.Reply = _ => new(HttpStatusCode.InternalServerError);
        await vm.RefreshCommand.ExecuteAsync(null);
        Assert.Empty(vm.Rooms);
        Assert.True(vm.HasError);
    }
}
