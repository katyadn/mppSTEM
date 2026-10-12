using Microclimate.Contracts.Common;

namespace Microclimate.Contracts.Rooms;

/// <summary>
/// Повна інформація про приміщення.
/// </summary>
/// <param name="Id">Унікальний ідентифікатор кімнати.</param>
/// <param name="BuildingId">Ідентифікатор будівлі.</param>
/// <param name="Name">Назва або номер кімнати.</param>
/// <param name="Floor">Поверх розміщення.</param>
/// <param name="AreaSqM">Площа приміщення у квадратних метрах (м²).</param>
/// <param name="Type">Призначення приміщення.</param>
/// <param name="CreatedAt">Час створення запису (UTC).</param>
public sealed record RoomResponse(
    Guid Id,
    Guid BuildingId,
    string Name,
    int Floor,
    double? AreaSqM,
    RoomType Type,
    DateTimeOffset CreatedAt);
