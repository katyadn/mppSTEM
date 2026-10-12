using Microclimate.Domain.Enums;

namespace Microclimate.Application.Analytics;

/// <summary>Обмеження запитів аналітики (docs/analytics.md, розділ 2).</summary>
public static class AnalyticsLimits
{
    /// <summary>Максимальний період запиту вимірів і агрегатів.</summary>
    public static readonly TimeSpan MaxPeriod = TimeSpan.FromDays(90);

    /// <summary>Максимальний період для погодинного ряду (не більше 744 точок).</summary>
    public static readonly TimeSpan MaxHourlyPeriod = TimeSpan.FromDays(31);
}

/// <summary>Параметри індексу комфорту (docs/analytics.md, розділ 3).</summary>
public static class ComfortIndexSettings
{
    /// <summary>За який останній період усереднюється поточне значення метрики.</summary>
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    /// <summary>Нижня межа категорії «Комфортно».</summary>
    public const double ComfortableFrom = 85;

    /// <summary>Нижня межа категорії «Прийнятно».</summary>
    public const double AcceptableFrom = 60;

    /// <summary>Вага метрики в індексі; метрики з вагою 0 не враховуються.</summary>
    public static double Weight(MetricType metric) => metric switch
    {
        MetricType.Temperature => 0.30,
        MetricType.Co2 => 0.30,
        MetricType.Humidity => 0.20,
        MetricType.Pm25 => 0.20,
        _ => 0,
    };
}
