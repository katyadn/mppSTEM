using Microclimate.Domain.Enums;

namespace Microclimate.Application.Analytics;

/// <summary>Обмеження запитів аналітики (docs/analytics.md, розділ 2).</summary>
public static class AnalyticsLimits
{
    public static readonly TimeSpan MaxPeriod = TimeSpan.FromDays(90);

    public static readonly TimeSpan MaxHourlyPeriod = TimeSpan.FromDays(31);
}

/// <summary>Параметри індексу комфорту (docs/analytics.md, розділ 3).</summary>
public static class ComfortIndexSettings
{
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    public const double ComfortableFrom = 85;

    public const double AcceptableFrom = 60;

    public static double Weight(MetricType metric) => metric switch
    {
        MetricType.Temperature => 0.30,
        MetricType.Co2 => 0.30,
        MetricType.Humidity => 0.20,
        MetricType.Pm25 => 0.20,
        _ => 0,
    };
}
