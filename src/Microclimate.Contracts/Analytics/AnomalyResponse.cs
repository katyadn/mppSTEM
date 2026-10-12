using Microclimate.Contracts.Common;

namespace Microclimate.Contracts.Analytics;

/// <summary>
/// Тип виявленої аномалії у часовому ряді вимірювань.
/// </summary>
public enum AnomalyType
{
    /// <summary>Статистичний викид за алгоритмом ковзного z-score.</summary>
    Outlier = 1,

    /// <summary>Фізично неможлива різка зміна (стрибок).</summary>
    Spike = 2,

    /// <summary>Зависання показника датчика на однаковому значенні.</summary>
    Stuck = 3,

    /// <summary>Пропуск / розрив у надходженні даних.</summary>
    Gap = 4,
}

/// <summary>
/// Опис виявленої аномалії сенсорних даних.
/// </summary>
/// <param name="Type">Тип аномалії.</param>
/// <param name="SensorId">Ідентифікатор датчика.</param>
/// <param name="Metric">Тип метрики.</param>
/// <param name="From">Початок інтервалу аномалії (UTC).</param>
/// <param name="To">Кінець інтервалу аномалії (UTC).</param>
/// <param name="Value">Фактичне аномальне значення (якщо застосовно).</param>
/// <param name="Expected">Очікуване нормальне значення (середнє вікна).</param>
/// <param name="Score">Показник відхилення (z-score або швидкість зміни).</param>
public sealed record AnomalyResponse(
    AnomalyType Type,
    Guid SensorId,
    MetricType Metric,
    DateTimeOffset From,
    DateTimeOffset To,
    double? Value,
    double? Expected,
    double? Score);
