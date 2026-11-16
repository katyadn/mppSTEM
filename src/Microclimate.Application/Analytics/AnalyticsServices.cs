using Microclimate.Domain.Enums;
using Microclimate.Domain.ValueObjects;

namespace Microclimate.Application.Analytics;

/// <summary>
/// Розрахунок індексу комфорту за значеннями показників і порогами.
/// </summary>
public interface IComfortIndexCalculator
{
    ComfortIndexResult Calculate(IReadOnlyCollection<ComfortInput> inputs);
}

/// <summary>Сценарії аналітики для ендпоінтів analytics/*.</summary>
public interface IAnalyticsService
{
    Task<IReadOnlyList<MetricSummary>> GetSummaryAsync(
        Guid roomId,
        MetricType? metric,
        DateRange range,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<SeriesPoint>> GetSeriesAsync(
        Guid roomId,
        MetricType metric,
        DateRange range,
        AggregationInterval interval,
        CancellationToken cancellationToken
    );

    Task<ComfortIndexResult> GetComfortIndexAsync(Guid roomId, CancellationToken cancellationToken);
}
