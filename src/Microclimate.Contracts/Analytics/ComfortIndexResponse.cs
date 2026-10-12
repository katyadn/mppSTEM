using Microclimate.Contracts.Common;

namespace Microclimate.Contracts.Analytics;

/// <summary>
/// Категорія якості мікроклімату за індексом комфорту.
/// </summary>
public enum ComfortCategory
{
    /// <summary>Дані відсутні за останні 15 хвилин.</summary>
    NoData = 0,

    /// <summary>Комфортні умови (CI >= 80).</summary>
    Comfortable = 1,

    /// <summary>Прийнятні умови (60 <= CI < 80).</summary>
    Acceptable = 2,

    /// <summary>Дискомфортні умови (CI < 60).</summary>
    Uncomfortable = 3,
}

/// <summary>
/// Оцінка окремої метрики в індексі комфорту.
/// </summary>
/// <param name="Metric">Тип метрики.</param>
/// <param name="Value">Поточне середнє значення за 15 хв.</param>
/// <param name="Score">Часткова оцінка від 0 до 100.</param>
/// <param name="Weight">Вага метрики у зваженому індексі.</param>
public sealed record ComfortScoreComponentResponse(
    MetricType Metric,
    double Value,
    double Score,
    double Weight);

/// <summary>
/// Зведений індекс комфорту приміщення.
/// </summary>
/// <param name="Index">Загальний бал комфорту (0–100); null, якщо недостатньо даних.</param>
/// <param name="Category">Категорія умов.</param>
/// <param name="Components">Деталізація за складовими метриками.</param>
public sealed record ComfortIndexResponse(
    double? Index,
    ComfortCategory Category,
    IReadOnlyList<ComfortScoreComponentResponse> Components);
