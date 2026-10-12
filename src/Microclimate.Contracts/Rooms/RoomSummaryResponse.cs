namespace Microclimate.Contracts.Rooms;

/// <summary>
/// Зведена інформація про кімнату для відображення в дашборді.
/// </summary>
/// <param name="Id">Ідентифікатор кімнати.</param>
/// <param name="BuildingId">Ідентифікатор будівлі.</param>
/// <param name="BuildingName">Назва будівлі.</param>
/// <param name="Name">Назва або номер кімнати.</param>
/// <param name="Floor">Поверх розміщення.</param>
/// <param name="Type">Призначення кімнати.</param>
/// <param name="Status">Статус якості мікроклімату ("Normal", "Warning", "Critical").</param>
/// <param name="LatestMetrics">Список останніх зафіксованих показників за кожною метрикою.</param>
public sealed record RoomSummaryResponse(
    Guid Id,
    Guid BuildingId,
    string BuildingName,
    string Name,
    int Floor,
    string? Type,
    string Status,
    IReadOnlyList<LatestMetricResponse> LatestMetrics);
