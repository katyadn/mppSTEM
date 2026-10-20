using Microclimate.Domain.Enums;

namespace Microclimate.Domain.Metrics;

/// <summary>
/// Довідник метрик. Нова метрика додається одним рядком тут (і значенням у MetricType).
/// Джерела порогів — docs/analytics.md, розділ 1.
/// </summary>
public static class MetricCatalog
{
    private static readonly Dictionary<MetricType, MetricDefinition> Definitions = new MetricDefinition[]
    {
        new(MetricType.Temperature, "°C", new(-40, 85), new(16, 20, 25, 28)),
        new(MetricType.Humidity, "%", new(0, 100), new(20, 40, 60, 75)),
        new(MetricType.Co2, "ppm", new(0, 10_000), new(null, null, 1200, 1750)),
        new(MetricType.Pm25, "µg/m³", new(0, 1_000), new(null, null, 15, 50)),
        new(MetricType.Noise, "dB(A)", new(0, 140), new(null, null, 50, 65)),
        new(MetricType.Light, "lux", new(0, 100_000), new(150, 300, null, null)),
    }.ToDictionary(d => d.Metric);

    public static IReadOnlyCollection<MetricDefinition> All => Definitions.Values;

    public static MetricDefinition Get(MetricType metric) =>
        Definitions.TryGetValue(metric, out var definition)
            ? definition
            : throw new ArgumentOutOfRangeException(nameof(metric), metric, "Невідома метрика.");
}
