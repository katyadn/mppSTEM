using Microclimate.Contracts.Common;

namespace Microclimate.Contracts.Measurements;

/// <summary>
/// Один вимір певної метрики від датчика.
/// </summary>
/// <param name="Metric">Тип метрики.</param>
/// <param name="Value">Значення в одиницях метрики (°C, %, ppm тощо).</param>
/// <param name="MeasuredAt">Момент фіксації виміру датчиком (UTC).</param>
public sealed record MeasurementItem(
    MetricType Metric,
    double Value,
    DateTimeOffset MeasuredAt);
