namespace Microclimate.Contracts.Buildings;

/// <summary>
/// Інформація про будівлю.
/// </summary>
/// <param name="Id">Унікальний ідентифікатор будівлі.</param>
/// <param name="Name">Назва будівлі.</param>
/// <param name="Address">Адреса будівлі.</param>
/// <param name="Description">Опис будівлі.</param>
/// <param name="CreatedAt">Час створення запису (UTC).</param>
public sealed record BuildingResponse(
    Guid Id,
    string Name,
    string? Address,
    string? Description,
    DateTimeOffset CreatedAt);
