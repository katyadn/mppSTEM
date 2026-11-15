using System.Net;
using System.Text.Json;
using Microclimate.Contracts.Realtime;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Microclimate.Client.Core.Services;

// Стан каналу відділено від доступності HTTP: під час reconnect залишається періодичне оновлення.
public enum LiveConnectionState { Connecting, Connected, Reconnecting, Offline, SignedOut }

// Один watcher належить видимій сторінці. Скасування припиняє transport, retries та підписки.
public sealed class ClientRealtime(IClientSettings settings, ClientSession session, TimeProvider clock)
{
    public HubConnection CreateConnection(Func<HttpMessageHandler>? handlerFactory = null, bool longPolling = false)
    {
        var server = new Uri(ApiAddress.Normalize(settings.ApiAddress));
        return new HubConnectionBuilder()
            .WithUrl(new Uri(server, "hubs/alerts"), options =>
            {
                options.AccessTokenProvider = async () =>
                {
                    if (ApiAddress.Normalize(settings.ApiAddress) != server.AbsoluteUri) return null;
                    return await session.TokenAsync(server.AbsoluteUri).ConfigureAwait(false);
                };
                options.HttpMessageHandlerFactory = original =>
                {
                    if (original is HttpClientHandler clientHandler) clientHandler.AllowAutoRedirect = false;
                    var inner = original;
                    if (handlerFactory is not null) { original.Dispose(); inner = handlerFactory(); }
                    return new ServerBoundHandler(server, inner);
                };
                if (longPolling) options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
            })
            .AddJsonProtocol(options => options.PayloadSerializerOptions = ClientJsonContext.Default.Options)
            .WithAutomaticReconnect([TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)])
            .Build();
    }

    // rooms читається повторно: пагінація огляду додає підписки без нового з'єднання.
    // refresh повертає false, якщо UI зайнятий; тоді подія не губиться.
    public async Task WatchAsync(HubConnection connection, Func<Task<IReadOnlyCollection<Guid>>> rooms, bool includeAlerts,
        Func<Task<bool>> refresh, Func<LiveConnectionState, Task> status, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(rooms);
        ArgumentNullException.ThrowIfNull(refresh);
        ArgumentNullException.ThrowIfNull(status);
        var server = ApiAddress.Normalize(settings.ApiAddress);
        var originalToken = await session.TokenAsync(server).ConfigureAwait(false);
        var dirty = 0;
        var generation = 0;
        var appliedGeneration = -1;
        var subscribed = new HashSet<Guid>();
        var allSubscribed = false;
        var lastRefresh = clock.GetUtcNow();
        var nextStart = DateTimeOffset.MinValue;
        var attempt = 0;
        LiveConnectionState? previousState = null;
        using var raised = connection.On<JsonElement>(HubMethods.AlertRaised, _ => Interlocked.Exchange(ref dirty, 1));
        using var acknowledged = connection.On<JsonElement>(HubMethods.AlertAcknowledged, _ => Interlocked.Exchange(ref dirty, 1));
        using var measured = connection.On<JsonElement>(HubMethods.MeasurementReceived, _ => Interlocked.Exchange(ref dirty, 1));
        Task Reconnected(string? _) { Interlocked.Increment(ref generation); Interlocked.Exchange(ref dirty, 1); return Task.CompletedTask; }
        connection.Reconnected += Reconnected;
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Не дозволяємо старому watcher продовжити роботу з іншим користувачем чи сервером.
                if (ApiAddress.Normalize(settings.ApiAddress) != server || originalToken is null ||
                    await session.TokenAsync(server).ConfigureAwait(false) != originalToken)
                {
                    await status(LiveConnectionState.SignedOut).ConfigureAwait(false);
                    return;
                }
                var now = clock.GetUtcNow();
                var currentState = LiveConnectionState.Offline;
                try
                {
                    if (connection.State == HubConnectionState.Disconnected && now >= nextStart)
                    {
                        currentState = LiveConnectionState.Connecting;
                        await connection.StartAsync(cancellationToken).ConfigureAwait(false);
                        Interlocked.Increment(ref generation); Interlocked.Exchange(ref dirty, 1); attempt = 0;
                    }
                    if (connection.State == HubConnectionState.Connected)
                    {
                        if (appliedGeneration != Volatile.Read(ref generation))
                        { subscribed.Clear(); allSubscribed = false; appliedGeneration = Volatile.Read(ref generation); }
                        if (includeAlerts && !allSubscribed)
                        { await connection.InvokeAsync("SubscribeToAllAlerts", cancellationToken).ConfigureAwait(false); allSubscribed = true; }
                        var desired = (await rooms().ConfigureAwait(false)).Where(id => id != Guid.Empty).ToHashSet();
                        foreach (var id in subscribed.Except(desired).ToArray())
                        { await connection.InvokeAsync("UnsubscribeFromRoom", id, cancellationToken).ConfigureAwait(false); subscribed.Remove(id); }
                        foreach (var id in desired.Except(subscribed))
                        { await connection.InvokeAsync("SubscribeToRoom", id, cancellationToken).ConfigureAwait(false); subscribed.Add(id); }
                        currentState = LiveConnectionState.Connected;
                    }
                    else if (connection.State == HubConnectionState.Reconnecting) currentState = LiveConnectionState.Reconnecting;
                }
                catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await session.ClearAsync().ConfigureAwait(false);
                    await status(LiveConnectionState.SignedOut).ConfigureAwait(false);
                    return;
                }
                catch (Exception exception) when (exception is HttpRequestException or HubException or InvalidOperationException or IOException ||
                    (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
                {
                    nextStart = now.AddSeconds(++attempt == 1 ? 2 : attempt == 2 ? 5 : 10);
                    currentState = LiveConnectionState.Offline;
                }
                if (previousState != currentState)
                { await status(currentState).ConfigureAwait(false); previousState = currentState; }
                // Максимум одне оновлення за 5 с; fallback також покриває сервер без AlertResolved.
                if ((Volatile.Read(ref dirty) != 0 && now - lastRefresh >= TimeSpan.FromSeconds(5)) || now - lastRefresh >= TimeSpan.FromSeconds(30))
                {
                    Interlocked.Exchange(ref dirty, 0);
                    if (await refresh().ConfigureAwait(false)) lastRefresh = now;
                    else Interlocked.Exchange(ref dirty, 1);
                }
                await Task.Delay(TimeSpan.FromSeconds(1), clock, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        finally
        {
            connection.Reconnected -= Reconnected;
            await connection.DisposeAsync().ConfigureAwait(false);
        }
    }

    // Блокує також redirect у JSON negotiation: bearer не може піти на інший origin.
    private sealed class ServerBoundHandler(Uri server, HttpMessageHandler inner) : DelegatingHandler(inner)
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri is not { } target || !server.IsBaseOf(target))
                throw new HttpRequestException("З'єднання намагалося перейти до іншого сервера.");
            return base.SendAsync(request, cancellationToken);
        }
    }
}
