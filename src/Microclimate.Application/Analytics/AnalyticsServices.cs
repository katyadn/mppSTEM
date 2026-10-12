using Microclimate.Domain.Enums;
using Microclimate.Domain.ValueObjects;

namespace Microclimate.Application.Analytics;

/// <summary>
/// Обчислення індексу комфорту (docs/analytics.md, розділ 3). Чиста функція без доступу до БД —
/// тестується unit-тестами на прикладах зі специфікації.
/// </summary>
public interface IComfortIndexCalculator
{
    ComfortIndexResult Calculate(IReadOnlyCollection<ComfortInput> inputs);
}

/// <summary>
/// Пошук аномалій у ряді одного датчика (docs/analytics.md, розділ 4). Чиста функція, O(n).
/// </summary>
public interface IAnomalyDetector
{
    /// <param name="series">Значення, впорядковані за часом.</param>
    /// <param name="expectedPeriod">Очікуваний інтервал між вимірами (для пошуку пропусків).</param>
    IReadOnlyList<Anomaly> Detect(Guid sensorId, MetricType metric, IReadOnlyList<TimedValue> series, TimeSpan expectedPeriod);
}

/// <summary>Сценарії аналітики для ендпоінтів analytics/* (Учасник 2 викликає з контролерів).</summary>
public interface IAnalyticsService
{
    Task<IReadOnlyList<MetricSummary>> GetSummaryAsync(Guid roomId, MetricType? metric, DateRange range, CancellationToken cancellationToken);

    /// <summary>Часовий ряд з порожніми кошиками для інтервалів без даних.</summary>
    Task<IReadOnlyList<SeriesPoint>> GetSeriesAsync(Guid roomId, MetricType metric, DateRange range, AggregationInterval interval, CancellationToken cancellationToken);

    /// <summary>Поточний індекс комфорту кімнати (за останні <see cref="ComfortIndexOptions.Window"/>).</summary>
    Task<ComfortIndexResult> GetComfortIndexAsync(Guid roomId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Anomaly>> GetAnomaliesAsync(Guid roomId, MetricType metric, DateRange range, CancellationToken cancellationToken);
}
