using Microclimate.Contracts.Common;

namespace Microclimate.Contracts.Sensors;

/// <summary>
/// Інформація про датчик мікроклімату.
/// </summary>
/// <param name="Id">Унікальний ідентифікатор датчика.</param>
/// <param name="RoomId">Ідентифікатор кімнати встановлення.</param>
/// <param name="SerialNumber">Серійний номер пристрою.</param>
/// <param name="Model">Модель / ревізія.</param>
/// <param name="IsActive">Ознака, чи датчик активний.</param>
/// <param name="Capabilities">Метрики, які вимірює датчик.</param>
/// <param name="InstalledAt">Час встановлення датчика в системі (UTC).</param>
/// <param name="LastSeenAt">Час останнього прийнятого виміру від датчика (UTC).</param>
public sealed record SensorResponse(
    Guid Id,
    Guid RoomId,
    string SerialNumber,
    string? Model,
    bool IsActive,
    IReadOnlyList<MetricType> Capabilities,
    DateTimeOffset InstalledAt,
    DateTimeOffset? LastSeenAt);
