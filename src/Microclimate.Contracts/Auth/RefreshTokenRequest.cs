namespace Microclimate.Contracts.Auth;

/// <summary>
/// Запит на оновлення пари JWT токенів за допомогою дійсного refresh-токена.
/// </summary>
/// <param name="RefreshToken">Діючий refresh-токен сесії.</param>
public sealed record RefreshTokenRequest(
    string RefreshToken);
