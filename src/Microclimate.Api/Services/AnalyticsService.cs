using Microclimate.Application.Abstractions.Persistence;
using Microclimate.Application.Analytics;
using Microclimate.Application.Common;
using Microclimate.Domain.Enums;
using Microclimate.Domain.Metrics;
using Microclimate.Domain.ValueObjects;

namespace Microclimate.Api.Services;

/// <summary>
/// Реалізація сервісу аналітики та агрегатів.
/// </summary>
public sealed class AnalyticsService(
    IMeasurementRepository measurements,
    IRoomRepository rooms,
    IThresholdRepository thresholds,
    IComfortIndexCalculator calculator,
    TimeProvider time) : IAnalyticsService
{
    public async Task<IReadOnlyList<MetricSummary>> GetSummaryAsync(
        Guid roomId,
        MetricType? metric,
        DateRange range,
        CancellationToken cancellationToken)
    {
        var room = await rooms.GetByIdAsync(roomId, cancellationToken);
        if (room is null)
        {
            throw NotFoundException.For("Кімната", roomId);
        }

        return await measurements.GetSummaryAsync(roomId, metric, range, cancellationToken);
    }

    public async Task<IReadOnlyList<SeriesPoint>> GetSeriesAsync(
        Guid roomId,
        MetricType metric,
        DateRange range,
        AggregationInterval interval,
        CancellationToken cancellationToken)
    {
        var room = await rooms.GetByIdAsync(roomId, cancellationToken);
        if (room is null)
        {
            throw NotFoundException.For("Кімната", roomId);
        }

        return await measurements.GetSeriesAsync(roomId, metric, range, interval, cancellationToken);
    }

    public async Task<ComfortIndexResult> GetComfortIndexAsync(
        Guid roomId,
        CancellationToken cancellationToken)
    {
        var room = await rooms.GetByIdAsync(roomId, cancellationToken);
        if (room is null)
        {
            throw NotFoundException.For("Кімната", roomId);
        }

        var latestMeasurements = await measurements.GetLatestByRoomAsync(roomId, cancellationToken);
        var now = time.GetUtcNow();
        var windowStart = now - ComfortIndexSettings.Window;

        var inputs = new List<ComfortInput>();
        foreach (var m in latestMeasurements)
        {
            // Враховуються лише виміри за останні 15 хвилин
            if (m.MeasuredAt < windowStart)
            {
                continue;
            }

            var customThreshold = await thresholds.GetAsync(roomId, m.Metric, cancellationToken);
            var limits = customThreshold?.Limits ?? DefaultThresholds.For(room.Type, m.Metric);

            inputs.Add(new ComfortInput(m.Metric, m.Value, limits));
        }

        return calculator.Calculate(inputs);
    }
}
