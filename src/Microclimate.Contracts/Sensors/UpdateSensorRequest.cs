using Microclimate.Contracts.Common;

namespace Microclimate.Contracts.Sensors;

/// <summary>
/// Запит адміністратора на оновлення параметрів датчика.
/// </summary>
/// <param name="RoomId">Ідентифікатор кімнати (для перенесення датчика в іншу кімнату).</param>
/// <param name="Model">Оновлена назва моделі.</param>
/// <param name="IsActive">Ознака активності датчика.</param>
public sealed record UpdateSensorRequest(
    Guid RoomId,
    string? Model,
    bool IsActive);
