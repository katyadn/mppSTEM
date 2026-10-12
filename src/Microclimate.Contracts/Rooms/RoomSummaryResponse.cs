using Microclimate.Contracts.Common;

namespace Microclimate.Contracts.Rooms;

/// <summary>
/// Зведена інформація про кімнату для відображення в дашборді та списках.
/// </summary>
/// <param name="Id">Ідентифікатор кімнати.</param>
/// <param name="BuildingId">Ідентифікатор будівлі.</param>
/// <param name="BuildingName">Назва будівлі.</param>
/// <param name="Name">Назва або номер кімнати.</param>
/// <param name="Floor">Поверх розміщення.</param>
/// <param name="Type">Призначення кімнати.</param>
/// <param name="Status">Загальний санітарно-кліматичний статус (Normal / Warning / Critical).</param>
/// <param name="LatestMetrics">Список останніх зафіксованих показників за кожною метрикою.</param>
public sealed record RoomSummaryResponse(
    Guid Id,
    Guid BuildingId,
    string BuildingName,
    string Name,
    int Floor,
    RoomType Type,
    RoomStatus Status,
    IReadOnlyList<LatestMetricResponse> LatestMetrics);
