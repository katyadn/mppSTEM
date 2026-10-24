using Microclimate.Api.Hubs;
using Microclimate.Application.Abstractions.Notifications;
using Microclimate.Application.Abstractions.Persistence;
using Microclimate.Contracts.Realtime;
using Microclimate.Domain.Alerts;

using Microsoft.AspNetCore.SignalR;

namespace Microclimate.Api.Services;

/// <summary>
/// Реалізація порту IAlertNotifier за допомогою SignalR (Dependency Inversion).
/// Транслює події підписникам кімнати та загальній групі all-alerts.
/// </summary>
public sealed class SignalRAlertNotifier(
    IHubContext<AlertsHub> hubContext,
    IRoomRepository rooms) : IAlertNotifier
{
    public async Task AlertRaisedAsync(Alert alert, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(alert);

        var room = await rooms.GetByIdAsync(alert.RoomId, cancellationToken);
        var roomName = room?.Name ?? "Приміщення";

        var metric = Enum.TryParse<Contracts.Common.MetricType>(alert.Metric.ToString(), out var m) ? m : Contracts.Common.MetricType.Temperature;
        var level = Enum.TryParse<Contracts.Common.AlertLevel>(alert.Level.ToString(), out var l) ? l : Contracts.Common.AlertLevel.Warning;

        var eventPayload = new AlertRaisedEvent(
            alert.Id,
            alert.RoomId,
            roomName,
            metric,
            level,
            alert.Value,
            alert.ThresholdValue,
            alert.Message,
            alert.RaisedAt);

        await hubContext.Clients
            .Groups($"room-{alert.RoomId}", "all-alerts")
            .SendAsync(HubMethods.AlertRaised, eventPayload, cancellationToken);
    }

    public async Task AlertAcknowledgedAsync(Alert alert, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(alert);

        await hubContext.Clients
            .Groups($"room-{alert.RoomId}", "all-alerts")
            .SendAsync(HubMethods.AlertAcknowledged, new { alert.Id, alert.RoomId, alert.AcknowledgedAt, alert.AcknowledgedByUserId }, cancellationToken);
    }

    public async Task AlertResolvedAsync(Alert alert, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(alert);

        await hubContext.Clients
            .Groups($"room-{alert.RoomId}", "all-alerts")
            .SendAsync("AlertResolved", new { alert.Id, alert.RoomId, alert.ResolvedAt }, cancellationToken);
    }
}
