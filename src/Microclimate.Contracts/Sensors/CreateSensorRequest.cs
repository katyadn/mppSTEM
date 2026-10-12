using Microclimate.Contracts.Common;

namespace Microclimate.Contracts.Sensors;

/// <summary>
/// Запит адміністратора на реєстрацію нового датчика.
/// </summary>
/// <param name="RoomId">Ідентифікатор кімнати встановлення.</param>
/// <param name="SerialNumber">Унікальний серійний номер датчика.</param>
/// <param name="Model">Модель / ревізія датчика.</param>
/// <param name="Capabilities">Список метрик, які підтримує датчик.</param>
public sealed record CreateSensorRequest(
    Guid RoomId,
    string SerialNumber,
    string? Model,
    IReadOnlyList<MetricType> Capabilities);
