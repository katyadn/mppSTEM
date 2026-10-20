using Microclimate.Domain.Enums;
using Microclimate.Domain.Thresholds;

namespace Microclimate.Domain.Metrics;

/// <summary>
/// Пороги за замовчуванням, якщо для кімнати не задано власних: значення з MetricCatalog
/// або окреме значення для типу кімнати (docs/analytics.md, розділ 1).
/// </summary>
public static class DefaultThresholds
{
    private static readonly Dictionary<(RoomType RoomType, MetricType Metric), ThresholdLimits> RoomTypeOverrides = new()
    {
        [(RoomType.ServerRoom, MetricType.Temperature)] = new(15, 18, 27, 32),
        [(RoomType.ServerRoom, MetricType.Humidity)] = new(8, 20, 70, 80),
    };

    public static ThresholdLimits For(RoomType roomType, MetricType metric) =>
        RoomTypeOverrides.TryGetValue((roomType, metric), out var limits) ? limits : MetricCatalog.Get(metric).DefaultLimits;
}
