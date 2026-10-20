using Microclimate.Domain.Users;

namespace Microclimate.Api.Services;

/// <summary>Генерація та валідація JWT-токенів.</summary>
public interface IJwtTokenService
{
    /// <summary>Генерує пару access + refresh токенів для користувача.</summary>
    (string AccessToken, string RefreshToken, int ExpiresIn) GenerateTokens(User user);

    /// <summary>
    /// Перевіряє підпис і термін дії refresh-токена.
    /// Повертає userId якщо токен валідний, або null — якщо ні.
    /// </summary>
    Guid? ValidateRefreshToken(string refreshToken);
}
