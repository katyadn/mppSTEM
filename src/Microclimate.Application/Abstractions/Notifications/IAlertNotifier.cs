using Microclimate.Domain.Alerts;

namespace Microclimate.Application.Abstractions.Notifications;

/// <summary>
/// Порт сповіщення клієнтів про зміни стану alert-ів. Application не знає про транспорт:
/// реалізацію через SignalR робить Учасник 2 у Microclimate.Api (Dependency Inversion).
/// Викликається лише після фіксації транзакції.
/// </summary>
public interface IAlertNotifier
{
    Task AlertRaisedAsync(Alert alert, CancellationToken cancellationToken);

    Task AlertAcknowledgedAsync(Alert alert, CancellationToken cancellationToken);

    Task AlertResolvedAsync(Alert alert, CancellationToken cancellationToken);
}
