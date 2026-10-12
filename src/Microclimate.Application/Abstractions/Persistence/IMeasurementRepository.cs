using Microclimate.Application.Analytics;
using Microclimate.Application.Common;
using Microclimate.Domain.Enums;
using Microclimate.Domain.Measurements;
using Microclimate.Domain.ValueObjects;

namespace Microclimate.Application.Abstractions.Persistence;

/// <summary>
/// Доступ до вимірів. Агрегати обчислюються в БД (GROUP BY), а не в пам'яті — див. docs/analytics.md, розділ 2.
/// </summary>
public interface IMeasurementRepository
{
    Task AddRangeAsync(IReadOnlyCollection<Measurement> measurements, CancellationToken cancellationToken);

    /// <summary>
    /// Ключі (метрика, час виміру), які вже є для датчика серед переданих — для ідемпотентного інгесту
    /// (UNIQUE SensorId, MetricType, MeasuredAt).
    /// </summary>
    Task<IReadOnlySet<(MetricType Metric, DateTimeOffset MeasuredAt)>> FindExistingAsync(
        Guid sensorId,
        IReadOnlyCollection<(MetricType Metric, DateTimeOffset MeasuredAt)> keys,
        CancellationToken cancellationToken);

    /// <summary>Сирі виміри метрики кімнати за період, впорядковані за часом.</summary>
    Task<PagedResult<Measurement>> GetByRoomAsync(
        Guid roomId,
        MetricType metric,
        DateRange range,
        PageRequest page,
        CancellationToken cancellationToken);

    /// <summary>Значення одного датчика за період для детекторів аномалій, впорядковані за часом.</summary>
    Task<IReadOnlyList<TimedValue>> GetSensorSeriesAsync(
        Guid sensorId,
        MetricType metric,
        DateRange range,
        CancellationToken cancellationToken);

    /// <summary>Потік вимірів для експорту без завантаження всього періоду в пам'ять.</summary>
    IAsyncEnumerable<Measurement> StreamByRoomAsync(Guid roomId, MetricType? metric, DateRange range, CancellationToken cancellationToken);

    /// <summary>min/max/avg/count/stdDev за кожною метрикою (або однією) за період.</summary>
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

    /// <summary>Останні значення кожної метрики кімнати (для Dashboard).</summary>
    Task<IReadOnlyList<Measurement>> GetLatestByRoomAsync(Guid roomId, CancellationToken cancellationToken);
}
