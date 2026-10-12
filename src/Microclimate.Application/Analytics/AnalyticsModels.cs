using Microclimate.Domain.Enums;
using Microclimate.Domain.Thresholds;

namespace Microclimate.Application.Analytics;

/// <summary>Розмір кошика часового ряду.</summary>
public enum AggregationInterval
{
    Hour = 1,
    Day = 2,
}

/// <summary>Агрегати метрики за період; при Count = 0 інші поля null.</summary>
public sealed record MetricSummary(MetricType Metric, int Count, double? Min, double? Max, double? Avg);

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
