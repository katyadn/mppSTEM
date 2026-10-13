using Microclimate.Domain.Enums;
using Microclimate.Domain.ValueObjects;

namespace Microclimate.Application.Analytics;

/// <summary>
/// Обчислення індексу комфорту (docs/analytics.md, розділ 3). Чиста функція без доступу до БД,
/// тому тестується на прикладах зі специфікації.
/// </summary>
public interface IComfortIndexCalculator
{
    ComfortIndexResult Calculate(IReadOnlyCollection<ComfortInput> inputs);
}

/// <summary>Сценарії аналітики для ендпоінтів analytics/* (контролери Учасника 2).</summary>
public interface IAnalyticsService
{
    Task<IReadOnlyList<MetricSummary>> GetSummaryAsync(Guid roomId, MetricType? metric, DateRange range, CancellationToken cancellationToken);

    Task<IReadOnlyList<SeriesPoint>> GetSeriesAsync(Guid roomId, MetricType metric, DateRange range, AggregationInterval interval, CancellationToken cancellationToken);

    Task<ComfortIndexResult> GetComfortIndexAsync(Guid roomId, CancellationToken cancellationToken);
}
