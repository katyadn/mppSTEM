using Microclimate.Contracts.Common;

namespace Microclimate.Contracts.Rooms;

/// <summary>
/// Останнє зафіксоване значення певної метрики для кімнати.
/// </summary>
/// <param name="Metric">Тип фізичної метрики.</param>
/// <param name="Value">Числове значення в одиницях метрики (°C, %, ppm тощо).</param>
/// <param name="MeasuredAt">Час вимірювання датчиком (UTC).</param>
/// <param name="Level">Рівень відхилення (null, якщо в межах норми).</param>
public sealed record LatestMetricResponse(
    MetricType Metric,
    double Value,
    DateTimeOffset MeasuredAt,
    AlertLevel? Level = null);
