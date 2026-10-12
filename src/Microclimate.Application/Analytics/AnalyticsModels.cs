using Microclimate.Domain.Enums;
using Microclimate.Domain.Thresholds;

namespace Microclimate.Application.Analytics;

/// <summary>Розмір кошика часового ряду.</summary>
public enum AggregationInterval
{
    Hour = 1,
    Day = 2,
}

/// <summary>Значення ряду в момент часу.</summary>
public readonly record struct TimedValue(DateTimeOffset At, double Value);

/// <summary>Агрегати метрики за період; при Count = 0 інші поля null.</summary>
public sealed record MetricSummary(MetricType Metric, int Count, double? Min, double? Max, double? Avg, double? StdDev);

/// <summary>Агрегати кошика часового ряду; порожній кошик має Count = 0 і null в агрегатах.</summary>
public sealed record SeriesPoint(DateTimeOffset BucketStart, int Count, double? Avg, double? Min, double? Max);

/// <summary>Категорія індексу комфорту.</summary>
public enum ComfortCategory
{
    NoData = 0,
    Comfortable = 1,
    Acceptable = 2,
    Uncomfortable = 3,
}

/// <summary>Поточне значення метрики та пороги, за якими її оцінювати.</summary>
public sealed record ComfortInput(MetricType Metric, double Value, ThresholdLimits Limits);

/// <summary>Часткова оцінка метрики (0–100).</summary>
public sealed record MetricComfortScore(MetricType Metric, double Value, double Score, double Weight);

/// <summary>Індекс комфорту кімнати; Index = null, якщо даних немає.</summary>
public sealed record ComfortIndexResult(double? Index, ComfortCategory Category, IReadOnlyList<MetricComfortScore> Components);

/// <summary>Тип аномалії часового ряду.</summary>
public enum AnomalyType
{
    /// <summary>Викид за ковзним z-score.</summary>
    Outlier = 1,

    /// <summary>Фізично неможлива швидкість зміни.</summary>
    Spike = 2,

    /// <summary>Однакове значення занадто довго.</summary>
    Stuck = 3,

    /// <summary>Пропуск даних.</summary>
    Gap = 4,
}

/// <summary>Знайдена аномалія ряду одного датчика.</summary>
/// <param name="Expected">Очікуване значення (середнє вікна), якщо застосовно.</param>
/// <param name="Score">z-score для викиду або швидкість зміни для стрибка.</param>
public sealed record Anomaly(
    AnomalyType Type,
    Guid SensorId,
    MetricType Metric,
    DateTimeOffset From,
    DateTimeOffset To,
    double? Value,
    double? Expected,
    double? Score);
