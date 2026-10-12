namespace Microclimate.Contracts.Buildings;

/// <summary>
/// Запит на створення нової будівлі.
/// </summary>
/// <param name="Name">Унікальна назва будівлі (наприклад, "Корпус 1").</param>
/// <param name="Address">Фізична адреса будівлі.</param>
/// <param name="Description">Додатковий опис призначення будівлі.</param>
public sealed record CreateBuildingRequest(
    string Name,
    string? Address = null,
    string? Description = null);
