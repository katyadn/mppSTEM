using System.Net;
using System.Text.Json;
using Microclimate.Api.Hubs;
using Microclimate.Client.Core.Services;
using Microclimate.Contracts.Realtime;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Microclimate.Client.Tests;

public sealed class RealtimeWorkflowTests
{
    [Fact]
    public async Task RealHubSubscriptionsRefreshRejoinAndStopAfterLogout()
    {
        await using var factory = new BackendWorkflowTests.SqliteBackendFactory();
        using var http = factory.CreateClient();
        var clock = new TestClock();
        var settings = new MemorySettings { ApiAddress = http.BaseAddress!.AbsoluteUri };
        var session = new ClientSession(new MemoryStore(), clock);
        var api = new ClientApi(http, settings, session, clock);
        await api.LoginAsync("operator@microclimate.local", "OperatorPassword123!", CancellationToken.None);
        var room = Assert.Single((await api.GetRoomsAsync(1, CancellationToken.None)).Items);
        var realtime = new ClientRealtime(settings, session, clock);
        var connection = realtime.CreateConnection(factory.Server.CreateHandler, longPolling: true);
        var connected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var refreshed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var signedOut = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(25));
        var allowRefresh = false;
        var watcher = realtime.WatchAsync(connection, () => Task.FromResult<IReadOnlyCollection<Guid>>([room.Id]), true,
            () => { if (!allowRefresh) return Task.FromResult(false); refreshed.TrySetResult(); return Task.FromResult(true); },
            state => { if (state == LiveConnectionState.Connected) connected.TrySetResult(); if (state == LiveConnectionState.SignedOut) signedOut.TrySetResult(); return Task.CompletedTask; }, lifetime.Token);
        try
        {
            await connected.Task.WaitAsync(lifetime.Token);
            var hub = factory.Services.GetRequiredService<IHubContext<AlertsHub>>();
            var measurementEvent = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var acknowledgedEvent = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var measurementSubscription = connection.On<JsonElement>(HubMethods.MeasurementReceived, _ => measurementEvent.TrySetResult());
            using var ackSubscription = connection.On<JsonElement>(HubMethods.AlertAcknowledged, _ => acknowledgedEvent.TrySetResult());
            clock.Now = clock.Now.AddSeconds(6);
            await hub.Clients.Group($"room-{room.Id}").SendAsync(HubMethods.MeasurementReceived, new { roomId = room.Id, value = 23 }, lifetime.Token);
            await measurementEvent.Task.WaitAsync(lifetime.Token);
            // Busy UI не втрачає подію; наступний цикл повторює refresh.
            await Task.Delay(1200, lifetime.Token);
            Assert.False(refreshed.Task.IsCompleted);
            allowRefresh = true;
            await refreshed.Task.WaitAsync(lifetime.Token);
            refreshed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await connection.StopAsync(lifetime.Token);
            while (connection.State != HubConnectionState.Connected) await Task.Delay(100, lifetime.Token);
            // Даємо завершити повторні Invoke-підписки після StartAsync.
            await Task.Delay(1200, lifetime.Token);
            clock.Now = clock.Now.AddSeconds(6);
            await hub.Clients.Group("all-alerts").SendAsync(HubMethods.AlertAcknowledged, new { id = Guid.NewGuid() }, lifetime.Token);
            await acknowledgedEvent.Task.WaitAsync(lifetime.Token);
            await refreshed.Task.WaitAsync(lifetime.Token);
            await session.ClearAsync();
            await signedOut.Task.WaitAsync(lifetime.Token);
            await watcher.WaitAsync(lifetime.Token);
            Assert.Equal(HubConnectionState.Disconnected, connection.State);
        }
        finally { await lifetime.CancelAsync(); await watcher; }
    }

    [Fact]
    public async Task NegotiationCannotSendCredentialsToAnotherOrigin()
    {
        using var f = new ClientFixture(); await f.SignInAsync();
        var requests = new List<Uri>();
        var realtime = new ClientRealtime(f.Settings, f.Session, f.Clock);
        await using var connection = realtime.CreateConnection(() => new FixtureHandler
        {
            ResponseFactory = (request, _) =>
            {
                requests.Add(request.RequestUri!);
                return Task.FromResult(ClientFixture.Json(new { url = "https://other.example.test/hubs/alerts", accessToken = "redirect-token" }));
            },
        }, longPolling: true);
        await Assert.ThrowsAsync<HttpRequestException>(() => connection.StartAsync());
        Assert.Single(requests); Assert.Equal("api.example.test", requests[0].Host);
    }

    [Fact]
    public async Task MissingHubFallsBackToPollingAndCancelsCleanly()
    {
        using var f = new ClientFixture(); await f.SignInAsync();
        var realtime = new ClientRealtime(f.Settings, f.Session, f.Clock);
        var connection = realtime.CreateConnection(() => new FixtureHandler { ResponseFactory = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)) }, longPolling: true);
        var offline = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var refreshed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var watcher = realtime.WatchAsync(connection, () => Task.FromResult<IReadOnlyCollection<Guid>>([]), true,
            () => { refreshed.TrySetResult(); return Task.FromResult(true); },
            state => { if (state == LiveConnectionState.Offline) offline.TrySetResult(); return Task.CompletedTask; }, lifetime.Token);
        try
        {
            await offline.Task.WaitAsync(lifetime.Token);
            f.Clock.Now = f.Clock.Now.AddSeconds(31);
            await refreshed.Task.WaitAsync(lifetime.Token);
        }
        finally { await lifetime.CancelAsync(); await watcher; }
        Assert.NotNull(f.Session.Current);
    }
}
