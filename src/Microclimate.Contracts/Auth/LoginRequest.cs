namespace Microclimate.Contracts.Auth;

/// <summary>
/// Запит на автентифікацію користувача за email та паролем.
/// </summary>
/// <param name="Email">Електронна пошта користувача.</param>
/// <param name="Password">Пароль облікового запису.</param>
public sealed record LoginRequest(
    string Email,
    string Password);
