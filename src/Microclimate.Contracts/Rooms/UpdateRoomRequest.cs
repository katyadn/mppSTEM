namespace Microclimate.Contracts.Rooms;

/// <summary>
/// Запит на оновлення параметрів приміщення.
/// </summary>
/// <param name="Name">Оновлена назва або номер кімнати.</param>
/// <param name="Floor">Оновлений поверх.</param>
/// <param name="AreaSqM">Оновлена площа приміщення (м²).</param>
/// <param name="Type">Оновлене призначення приміщення.</param>
public sealed record UpdateRoomRequest(
    string Name,
    int Floor,
    double? AreaSqM = null,
    string? Type = null);
