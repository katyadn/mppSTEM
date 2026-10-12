using Microclimate.Domain.Enums;
using Microclimate.Domain.Thresholds;

namespace Microclimate.Domain.Metrics;

/// <summary>
/// Пороги за замовчуванням, якщо для кімнати не задано власних.
/// Значення та джерела — docs/analytics.md, розділ 1.
/// </summary>
public static class DefaultThresholds
{
    private static readonly ThresholdLimits Temperature = new(16, 20, 25, 28);
    private static readonly ThresholdLimits Humidity = new(20, 40, 60, 75);
    private static readonly ThresholdLimits Co2 = new(null, null, 1200, 1750);
    private static readonly ThresholdLimits Pm25 = new(null, null, 15, 50);
    private static readonly ThresholdLimits Noise = new(null, null, 50, 65);
    private static readonly ThresholdLimits Light = new(150, 300, null, null);

    private static readonly ThresholdLimits ServerRoomTemperature = new(15, 18, 27, 32);
    private static readonly ThresholdLimits ServerRoomHumidity = new(8, 20, 70, 80);

    /// <summary>Поріг за замовчуванням для типу кімнати й метрики.</summary>
    public static ThresholdLimits For(RoomType roomType, MetricType metric) => (roomType, metric) switch
    {
        (RoomType.ServerRoom, MetricType.Temperature) => ServerRoomTemperature,
        (RoomType.ServerRoom, MetricType.Humidity) => ServerRoomHumidity,
        (_, MetricType.Temperature) => Temperature,
        (_, MetricType.Humidity) => Humidity,
        (_, MetricType.Co2) => Co2,
        (_, MetricType.Pm25) => Pm25,
        (_, MetricType.Noise) => Noise,
        (_, MetricType.Light) => Light,
        _ => throw new ArgumentOutOfRangeException(nameof(metric), metric, "Невідома метрика."),
    };
}
