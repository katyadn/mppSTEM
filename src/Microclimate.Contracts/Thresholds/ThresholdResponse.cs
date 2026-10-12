using Microclimate.Contracts.Common;

namespace Microclimate.Contracts.Thresholds;

/// <summary>
/// Інформація про встановлені граничні пороги для метрики приміщення.
/// </summary>
/// <param name="Id">Унікальний ідентифікатор запису порогу.</param>
/// <param name="RoomId">Ідентифікатор кімнати.</param>
/// <param name="Metric">Тип метрики.</param>
/// <param name="WarningMin">Нижня межа попередження.</param>
/// <param name="WarningMax">Верхня межа попередження.</param>
/// <param name="CriticalMin">Нижня критична межа.</param>
/// <param name="CriticalMax">Верхня критична межа.</param>
public sealed record ThresholdResponse(
    Guid Id,
    Guid RoomId,
    MetricType Metric,
    double? WarningMin,
    double? WarningMax,
    double? CriticalMin,
    double? CriticalMax);
