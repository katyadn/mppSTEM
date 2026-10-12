using Microclimate.Contracts.Common;

namespace Microclimate.Contracts.Analytics;

/// <summary>
/// Агреговані статистичні показники метрики за період.
/// </summary>
/// <param name="Metric">Тип метрики.</param>
/// <param name="Count">Кількість вимірів (якщо 0, числові поля дорівнюють null).</param>
/// <param name="Min">Мінімальне значення в одиницях метрики.</param>
/// <param name="Max">Максимальне значення в одиницях метрики.</param>
/// <param name="Avg">Середнє арифметичне значення в одиницях метрики.</param>
/// <param name="StdDev">Стандартне відхилення сукупності в одиницях метрики.</param>
public sealed record MetricSummaryResponse(
    MetricType Metric,
    int Count,
    double? Min,
    double? Max,
    double? Avg,
    double? StdDev);
