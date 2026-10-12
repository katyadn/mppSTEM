namespace Microclimate.Contracts.Buildings;

/// <summary>
/// Запит на оновлення даних існуючої будівлі.
/// </summary>
/// <param name="Name">Оновлена назва будівлі.</param>
/// <param name="Address">Оновлена фізична адреса будівлі.</param>
/// <param name="Description">Оновлений опис будівлі.</param>
public sealed record UpdateBuildingRequest(
    string Name,
    string? Address = null,
    string? Description = null);
