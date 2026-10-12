namespace Microclimate.Contracts.Auth;

/// <summary>
/// Відповідь з автентифікаційними токенами.
/// </summary>
/// <param name="AccessToken">Короткоживучий JWT токен доступу (15 хв).</param>
/// <param name="RefreshToken">Довгоживучий токен оновлення сесії (7 діб).</param>
/// <param name="ExpiresIn">Час дії access-токена в секундах (зазвичай 900 с).</param>
/// <param name="TokenType">Тип токена (за замовчуванням "Bearer").</param>
public sealed record TokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType = "Bearer");
