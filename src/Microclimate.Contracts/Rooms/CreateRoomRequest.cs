namespace Microclimate.Contracts.Rooms;

/// <summary>
/// Запит на створення нового приміщення.
/// </summary>
/// <param name="BuildingId">Ідентифікатор будівлі, якій належить кімната.</param>
/// <param name="Name">Назва або номер кімнати (наприклад, "101").</param>
/// <param name="Floor">Поверх розміщення.</param>
/// <param name="AreaSqM">Площа приміщення у квадратних метрах (м²).</param>
/// <param name="Type">Призначення приміщення (наприклад, "Office", "Classroom").</param>
public sealed record CreateRoomRequest(
    Guid BuildingId,
    string Name,
    int Floor,
    double? AreaSqM = null,
    string? Type = null);
