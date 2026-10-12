namespace Microclimate.Contracts.Thresholds;

/// <summary>
/// Запит на встановлення або оновлення порогів якості мікроклімату для кімнати.
/// </summary>
/// <param name="WarningMin">Нижня межа попередження (в одиницях метрики); null, якщо нижня межа не контролюється.</param>
/// <param name="WarningMax">Верхня межа попередження (в одиницях метрики); null, якщо верхня межа не контролюється.</param>
/// <param name="CriticalMin">Нижня критична межа (в одиницях метрики).</param>
/// <param name="CriticalMax">Верхня критична межа (в одиницях метрики).</param>
public sealed record UpdateThresholdRequest(
    double? WarningMin = null,
    double? WarningMax = null,
    double? CriticalMin = null,
    double? CriticalMax = null);
