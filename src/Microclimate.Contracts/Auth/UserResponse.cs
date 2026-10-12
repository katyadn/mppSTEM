using Microclimate.Contracts.Common;

namespace Microclimate.Contracts.Auth;

/// <summary>
/// Інформація про профіль користувача.
/// </summary>
/// <param name="Id">Унікальний ідентифікатор користувача.</param>
/// <param name="Email">Електронна пошта користувача.</param>
/// <param name="Role">Роль користувача в системі.</param>
/// <param name="CreatedAt">Час реєстрації користувача (UTC).</param>
public sealed record UserResponse(
    Guid Id,
    string Email,
    UserRole Role,
    DateTimeOffset CreatedAt);
