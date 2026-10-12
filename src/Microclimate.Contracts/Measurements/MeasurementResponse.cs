using Microclimate.Contracts.Common;

namespace Microclimate.Contracts.Measurements;

/// <summary>
/// Інформація про збережений вимір показника.
/// </summary>
/// <param name="Id">Унікальний ідентифікатор виміру.</param>
/// <param name="RoomId">Ідентифікатор кімнати.</param>
/// <param name="SensorId">Ідентифікатор датчика.</param>
/// <param name="Metric">Тип метрики.</param>
/// <param name="Value">Значення в одиницях метрики (°C, %, ppm тощо).</param>
/// <param name="MeasuredAt">Час вимірювання датчиком (UTC).</param>
/// <param name="CreatedAt">Час збереження на сервері (UTC).</param>
public sealed record MeasurementResponse(
    Guid Id,
    Guid RoomId,
    Guid SensorId,
    MetricType Metric,
    double Value,
    DateTimeOffset MeasuredAt,
    DateTimeOffset CreatedAt);
