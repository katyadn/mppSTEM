using Microclimate.Contracts.Common;

namespace Microclimate.Contracts.Users;

/// <summary>
/// Запит адміністратора на створення нового користувача.
/// </summary>
/// <param name="Email">Електронна пошта нового користувача.</param>
/// <param name="Password">Початковий пароль облікового запису.</param>
/// <param name="Role">Призначена роль користувача в системі.</param>
public sealed record CreateUserRequest(
    string Email,
    string Password,
    UserRole Role);
