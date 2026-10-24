using Microclimate.Api.Hubs;
using Microclimate.Application.Abstractions.Notifications;
using Microclimate.Application.Abstractions.Persistence;
using Microclimate.Application.Common;
using Microclimate.Application.Measurements;
using Microclimate.Contracts.Realtime;
using Microclimate.Domain.Alerts;
using Microclimate.Domain.Measurements;
using Microclimate.Domain.Metrics;
using Microclimate.Domain.Thresholds;

using Microsoft.AspNetCore.SignalR;

namespace Microclimate.Api.Services;

/// <summary>
/// Сервіс прийому пакетних вимірів від датчиків з перевіркою порогів та генерацією сповіщень.
/// </summary>
public sealed class MeasurementIngestionService(
    ISensorRepository sensors,
    IRoomRepository rooms,
    IMeasurementRepository measurements,
    IThresholdRepository thresholds,
    IAlertRepository alerts,
    IAlertNotifier alertNotifier,
    IUnitOfWork unitOfWork,
    IHubContext<AlertsHub> hubContext,
    TimeProvider time) : IMeasurementIngestionService
{
    public async Task<IngestionResult> IngestAsync(
        Guid sensorId,
        IReadOnlyCollection<MeasurementInput> items,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count == 0)
        {
            return new IngestionResult(0, 0, 0);
        }

        if (items.Count > IMeasurementIngestionService.MaxBatchSize)
        {
            throw new ValidationException(
                nameof(items),
                $"Пакет містить {items.Count} вимірів, що перевищує ліміт {IMeasurementIngestionService.MaxBatchSize}.");
        }

        var sensor = await sensors.GetByIdAsync(sensorId, cancellationToken);
        if (sensor is null)
        {
            throw NotFoundException.For("Датчик", sensorId);
        }

        sensor.EnsureActive();

        var room = await rooms.GetByIdAsync(sensor.RoomId, cancellationToken);
        if (room is null)
        {
            throw NotFoundException.For("Кімната", sensor.RoomId);
        }

        var now = time.GetUtcNow();

        // Валідуємо і створюємо сутності Measurement
        var measurementEntities = new List<Measurement>(items.Count);
        foreach (var item in items)
        {
            var entity = Measurement.Record(sensor, item.Metric, item.Value, item.MeasuredAt, now);
            measurementEntities.Add(entity);
        }

        var alertsRaised = 0;
        var alertsResolved = 0;
        var raisedAlerts = new List<Alert>();
        var resolvedAlerts = new List<Alert>();

        // Оцінка порогів для останнього виміру кожної метрики в пакеті
        var latestPerMetric = items
            .GroupBy(i => i.Metric)
            .Select(g => g.OrderByDescending(x => x.MeasuredAt).First())
            .ToList();

        foreach (var latest in latestPerMetric)
        {
            var customThreshold = await thresholds.GetAsync(sensor.RoomId, latest.Metric, cancellationToken);
            var limits = customThreshold?.Limits ?? DefaultThresholds.For(room.Type, latest.Metric);

            var level = limits.Evaluate(latest.Value);
            var existingAlert = await alerts.GetUnresolvedAsync(sensor.RoomId, latest.Metric, cancellationToken);

            if (level.HasValue)
            {
                var exceededBound = limits.ExceededBound(latest.Value) ?? latest.Value;
                var unit = MetricLimits.Unit(latest.Metric);
                var message = $"Перевищено поріг {latest.Metric} у кімнаті '{room.Name}': {latest.Value:0.##} {unit} (межа {exceededBound:0.##} {unit}).";

                if (existingAlert is null)
                {
                    var newAlert = Alert.Raise(
                        sensor.RoomId,
                        sensor.Id,
                        latest.Metric,
                        level.Value,
                        latest.Value,
                        exceededBound,
                        message,
                        now);

                    alerts.Add(newAlert);
                    raisedAlerts.Add(newAlert);
                    alertsRaised++;
                }
                else if (existingAlert.Level != level.Value)
                {
                    existingAlert.Resolve(now);
                    resolvedAlerts.Add(existingAlert);
                    alertsResolved++;

                    var newAlert = Alert.Raise(
                        sensor.RoomId,
                        sensor.Id,
                        latest.Metric,
                        level.Value,
                        latest.Value,
                        exceededBound,
                        message,
                        now);

                    alerts.Add(newAlert);
                    raisedAlerts.Add(newAlert);
                    alertsRaised++;
                }
            }
            else if (existingAlert is not null)
            {
                existingAlert.Resolve(now);
                resolvedAlerts.Add(existingAlert);
                alertsResolved++;
            }
        }

        // Зберігаємо в одній транзакції
        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await measurements.AddRangeAsync(measurementEntities, ct);
            sensor.MarkSeen(now);
            await unitOfWork.SaveChangesAsync(ct);
        }, cancellationToken);

        // Оповіщення SignalR після коміту
        foreach (var alert in raisedAlerts)
        {
            await alertNotifier.AlertRaisedAsync(alert, cancellationToken);
        }

        foreach (var alert in resolvedAlerts)
        {
            await alertNotifier.AlertResolvedAsync(alert, cancellationToken);
        }

        // Сповіщення про надходження вимірів у кімнату
        foreach (var item in latestPerMetric)
        {
            var metricType = Enum.TryParse<Contracts.Common.MetricType>(item.Metric.ToString(), out var mt) ? mt : Contracts.Common.MetricType.Temperature;
            var eventPayload = new MeasurementReceivedEvent(sensor.RoomId, metricType, item.Value, item.MeasuredAt);
            await hubContext.Clients.Group($"room-{sensor.RoomId}").SendAsync(HubMethods.MeasurementReceived, eventPayload, cancellationToken);
        }

        return new IngestionResult(measurementEntities.Count, alertsRaised, alertsResolved);
    }
}
