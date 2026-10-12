using Microclimate.Contracts.Common;

namespace Microclimate.Contracts.Users;

/// <summary>
/// Запит адміністратора на зміну ролі існуючого користувача.
/// </summary>
/// <param name="Role">Нова роль користувача.</param>
public sealed record ChangeRoleRequest(
    UserRole Role);
