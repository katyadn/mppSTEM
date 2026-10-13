using Microclimate.Application.Analytics;
using Microclimate.Application.Common;
using Microclimate.Domain.Enums;
using Microclimate.Domain.Measurements;
using Microclimate.Domain.ValueObjects;

namespace Microclimate.Application.Abstractions.Persistence;

/// <summary>
/// Доступ до вимірів. Виміри кімнати — це виміри її датчиків (JOIN Measurements → Sensors).
/// Агрегати обчислюються в БД (GROUP BY), а не в пам'яті.
/// </summary>
public interface IMeasurementRepository
{
    Task AddRangeAsync(IReadOnlyCollection<Measurement> measurements, CancellationToken cancellationToken);

    Task<PagedResult<Measurement>> GetByRoomAsync(
        Guid roomId,
        MetricType metric,
        DateRange range,
        PageRequest page,
        CancellationToken cancellationToken);

    IAsyncEnumerable<Measurement> StreamByRoomAsync(Guid roomId, MetricType? metric, DateRange range, CancellationToken cancellationToken);

    Task<IReadOnlyList<MetricSummary>> GetSummaryAsync(
        Guid roomId,
        MetricType? metric,
        DateRange range,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SeriesPoint>> GetSeriesAsync(
        Guid roomId,
        MetricType metric,
        DateRange range,
        AggregationInterval interval,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Measurement>> GetLatestByRoomAsync(Guid roomId, CancellationToken cancellationToken);
}
