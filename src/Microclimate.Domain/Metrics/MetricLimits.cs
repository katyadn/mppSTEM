using Microclimate.Domain.Enums;
using Microclimate.Domain.ValueObjects;

namespace Microclimate.Domain.Metrics;

/// <summary>
/// Фізично можливі межі значень датчиків. Значення поза межами вважаються помилкою даних,
/// а не поганим мікрокліматом (для цього є пороги).
/// </summary>
public static class MetricLimits
{
    private static readonly MetricRange TemperatureRange = new(-40, 85);
    private static readonly MetricRange HumidityRange = new(0, 100);
    private static readonly MetricRange Co2Range = new(0, 10_000);
    private static readonly MetricRange Pm25Range = new(0, 1_000);
    private static readonly MetricRange NoiseRange = new(0, 140);
    private static readonly MetricRange LightRange = new(0, 100_000);

    /// <summary>Допустимий фізичний діапазон метрики.</summary>
    public static MetricRange For(MetricType metric) => metric switch
    {
        MetricType.Temperature => TemperatureRange,
        MetricType.Humidity => HumidityRange,
        MetricType.Co2 => Co2Range,
        MetricType.Pm25 => Pm25Range,
        MetricType.Noise => NoiseRange,
        MetricType.Light => LightRange,
        _ => throw new ArgumentOutOfRangeException(nameof(metric), metric, "Невідома метрика."),
    };

    /// <summary>Одиниця вимірювання метрики.</summary>
    public static string Unit(MetricType metric) => metric switch
    {
        MetricType.Temperature => "°C",
        MetricType.Humidity => "%",
        MetricType.Co2 => "ppm",
        MetricType.Pm25 => "µg/m³",
        MetricType.Noise => "dB(A)",
        MetricType.Light => "lux",
        _ => throw new ArgumentOutOfRangeException(nameof(metric), metric, "Невідома метрика."),
    };
}
