using Microclimate.Domain.Enums;

namespace Microclimate.Application.Analytics;

/// <summary>Обмеження запитів аналітики (docs/analytics.md, розділ 2).</summary>
public static class AnalyticsLimits
{
    /// <summary>Максимальний період запиту вимірів і агрегатів.</summary>
    public static readonly TimeSpan MaxPeriod = TimeSpan.FromDays(90);

    /// <summary>Максимальний період для погодинного ряду (≤ 744 точок).</summary>
    public static readonly TimeSpan MaxHourlyPeriod = TimeSpan.FromDays(31);
}

/// <summary>Параметри індексу комфорту (docs/analytics.md, розділ 3). Прив'язуються до конфігурації.</summary>
public sealed class ComfortIndexOptions
{
    /// <summary>Вікно усереднення поточного значення метрики.</summary>
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>Ваги метрик; метрика з вагою 0 не враховується.</summary>
    public Dictionary<MetricType, double> Weights { get; init; } = new()
    {
        [MetricType.Temperature] = 0.30,
        [MetricType.Co2] = 0.30,
        [MetricType.Humidity] = 0.20,
        [MetricType.Pm25] = 0.20,
        [MetricType.Noise] = 0,
        [MetricType.Light] = 0,
    };

    /// <summary>Нижня межа категорії «Комфортно».</summary>
    public double ComfortableFrom { get; set; } = 85;

    /// <summary>Нижня межа категорії «Прийнятно».</summary>
    public double AcceptableFrom { get; set; } = 60;

    /// <summary>Частка від межі Warning, яка використовується як D, якщо межі Critical немає.</summary>
    public double MissingCriticalSpanRatio { get; set; } = 0.2;
}

/// <summary>Параметри детекторів аномалій (docs/analytics.md, розділ 4). Прив'язуються до конфігурації.</summary>
public sealed class AnomalyDetectionOptions
{
    /// <summary>Розмір ковзного вікна z-score.</summary>
    public int WindowSize { get; set; } = 30;

    /// <summary>Мінімум точок у вікні для перевірки.</summary>
    public int MinWindowSize { get; set; } = 10;

    /// <summary>Поріг |z| для викиду.</summary>
    public double ZScoreThreshold { get; set; } = 3;

    /// <summary>Кількість однакових значень поспіль для «завислого» датчика.</summary>
    public int StuckCount { get; set; } = 120;

    /// <summary>Пропуск — інтервал більший за цей множник очікуваного періоду.</summary>
    public double GapFactor { get; set; } = 3;

    /// <summary>Мінімальна значуща зміна метрики ε.</summary>
    public Dictionary<MetricType, double> Epsilon { get; init; } = new()
    {
        [MetricType.Temperature] = 0.5,
        [MetricType.Humidity] = 2,
        [MetricType.Co2] = 50,
        [MetricType.Pm25] = 5,
        [MetricType.Noise] = 3,
        [MetricType.Light] = 50,
    };

    /// <summary>Максимальна правдоподібна швидкість зміни за хвилину; метрики без значення не перевіряються.</summary>
    public Dictionary<MetricType, double> MaxRatePerMinute { get; init; } = new()
    {
        [MetricType.Temperature] = 2,
        [MetricType.Humidity] = 10,
        [MetricType.Co2] = 500,
        [MetricType.Pm25] = 100,
    };
}

/// <summary>Параметри генерації сповіщень (docs/analytics.md, розділ 5).</summary>
public sealed class AlertingOptions
{
    /// <summary>Скільки вимірів поспіль у нормі потрібно для закриття сповіщення (гістерезис).</summary>
    public int ResolveAfterNormalCount { get; set; } = 3;

    /// <summary>Вікно ковзного середнього для PM2.5 перед порівнянням з порогом.</summary>
    public TimeSpan Pm25AveragingWindow { get; set; } = TimeSpan.FromMinutes(60);
}
