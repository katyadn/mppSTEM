using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Microclimate.Api.Hubs;

/// <summary>
/// Хаб SignalR для отримання подій мікроклімату та сповіщень у реальному часі.
/// Підтримує підписку на конкретну кімнату (room-{roomId}) або всі сповіщення (all-alerts).
/// </summary>
[Authorize]
public sealed class AlertsHub : Hub
{
    public async Task SubscribeToRoom(Guid roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"room-{roomId}");
    }

    public async Task UnsubscribeFromRoom(Guid roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room-{roomId}");
    }

    public async Task SubscribeToAllAlerts()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "all-alerts");
    }

    public async Task UnsubscribeFromAllAlerts()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "all-alerts");
    }
}
