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

    /// <summary>Виміри метрики кімнати за період, впорядковані за часом.</summary>
    Task<PagedResult<Measurement>> GetByRoomAsync(
        Guid roomId,
        MetricType metric,
        DateRange range,
        PageRequest page,
        CancellationToken cancellationToken);

    /// <summary>Потік вимірів для експорту без завантаження всього періоду в пам'ять.</summary>
    IAsyncEnumerable<Measurement> StreamByRoomAsync(Guid roomId, MetricType? metric, DateRange range, CancellationToken cancellationToken);

    /// <summary>min/max/avg/count за кожною метрикою (або однією) за період.</summary>
    Task<IReadOnlyList<MetricSummary>> GetSummaryAsync(
        Guid roomId,
        MetricType? metric,
        DateRange range,
        CancellationToken cancellationToken);

    /// <summary>Агрегати за кошиками часу. Порожні кошики БД не повертає — їх додає сервіс аналітики.</summary>
    Task<IReadOnlyList<SeriesPoint>> GetSeriesAsync(
        Guid roomId,
        MetricType metric,
        DateRange range,
        AggregationInterval interval,
        CancellationToken cancellationToken);

    /// <summary>Останнє значення кожної метрики кімнати (для Dashboard).</summary>
    Task<IReadOnlyList<Measurement>> GetLatestByRoomAsync(Guid roomId, CancellationToken cancellationToken);
}
