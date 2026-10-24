using Microclimate.Application.Abstractions.Persistence;
using Microclimate.Application.Common;
using Microclimate.Domain.Enums;
using Microclimate.Domain.Metrics;
using Microclimate.Domain.Rooms;
using Microclimate.Domain.ValueObjects;

namespace Microclimate.Application.Analytics;

/// <summary>Зведення, часовий ряд та індекс комфорту кімнати (docs/analytics.md, розділи 2–3).</summary>
public sealed class AnalyticsService(
    IRoomRepository rooms,
    IMeasurementRepository measurements,
    IThresholdRepository thresholds,
    IComfortIndexCalculator calculator,
    TimeProvider time) : IAnalyticsService
{
    public async Task<IReadOnlyList<MetricSummary>> GetSummaryAsync(Guid roomId, MetricType? metric, DateRange range, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(range);
        EnsurePeriod(range, AnalyticsLimits.MaxPeriod);
        await GetRoomAsync(roomId, cancellationToken);

        var found = (await measurements.GetSummaryAsync(roomId, metric, range, cancellationToken)).ToDictionary(s => s.Metric);
        IEnumerable<MetricType> metrics = metric is { } single ? [single] : Enum.GetValues<MetricType>();

        return metrics
            .Select(m => found.TryGetValue(m, out var summary) ? summary : new MetricSummary(m, 0, null, null, null))
            .ToList();
    }

    public async Task<IReadOnlyList<SeriesPoint>> GetSeriesAsync(Guid roomId, MetricType metric, DateRange range, AggregationInterval interval, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(range);
        EnsurePeriod(range, interval == AggregationInterval.Hour ? AnalyticsLimits.MaxHourlyPeriod : AnalyticsLimits.MaxPeriod);
        await GetRoomAsync(roomId, cancellationToken);

        var found = (await measurements.GetSeriesAsync(roomId, metric, range, interval, cancellationToken)).ToDictionary(p => p.BucketStart);
        var step = AggregationBuckets.Step(interval);
        var result = new List<SeriesPoint>();

        // Інтервали без даних повертаються з Count = 0, щоб графік показував розрив.
        for (var bucket = AggregationBuckets.Floor(range.From, interval); bucket < range.To; bucket += step)
        {
            result.Add(found.TryGetValue(bucket, out var point) ? point : new SeriesPoint(bucket, 0, null, null, null));
        }

        return result;
    }

    public async Task<ComfortIndexResult> GetComfortIndexAsync(Guid roomId, CancellationToken cancellationToken)
    {
        var room = await GetRoomAsync(roomId, cancellationToken);
        var now = time.GetUtcNow();
        var window = DateRange.Create(now - ComfortIndexSettings.Window, now);

        var averages = await measurements.GetSummaryAsync(roomId, null, window, cancellationToken);
        var custom = (await thresholds.ListByRoomAsync(roomId, cancellationToken)).ToDictionary(t => t.Metric);

        var inputs = averages
            .Where(s => s.Avg is not null)
            .Select(s => new ComfortInput(
                s.Metric,
                s.Avg!.Value,
                custom.TryGetValue(s.Metric, out var threshold) ? threshold.Limits : DefaultThresholds.For(room.Type, s.Metric)))
            .ToList();

        return calculator.Calculate(inputs);
    }

    private async Task<Room> GetRoomAsync(Guid roomId, CancellationToken cancellationToken) =>
        await rooms.GetByIdAsync(roomId, cancellationToken) ?? throw NotFoundException.For("Кімната", roomId);

    private static void EnsurePeriod(DateRange range, TimeSpan max)
    {
        if (range.Duration > max)
        {
            throw new ValidationException("range", $"Період не може перевищувати {max.TotalDays:0} дн.");
        }
    }
}
