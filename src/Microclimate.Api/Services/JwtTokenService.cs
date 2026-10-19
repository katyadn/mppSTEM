using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Microclimate.Domain.Users;

using Microsoft.IdentityModel.Tokens;

namespace Microclimate.Api.Services;

/// <summary>Реалізація JWT-токенів на основі HMAC-SHA256.</summary>
internal sealed class JwtTokenService : IJwtTokenService
{
    private const int AccessTokenLifetimeSeconds = 900; // 15 хв
    private const int RefreshTokenLifetimeDays = 7;

    private readonly string _issuer;
    private readonly string _audience;
    private readonly SymmetricSecurityKey _signingKey;
    private readonly TimeProvider _time;

    public JwtTokenService(IConfiguration configuration, TimeProvider time)
    {
        _time = time;
        var jwtSection = configuration.GetSection("Jwt");
        _issuer = jwtSection["Issuer"] ?? "microclimate-api";
        _audience = jwtSection["Audience"] ?? "microclimate-client";
        var secret = jwtSection["SecretKey"]
            ?? throw new InvalidOperationException("Jwt:SecretKey is not configured.");
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
    }

    public (string AccessToken, string RefreshToken, int ExpiresIn) GenerateTokens(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var now = _time.GetUtcNow();
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now.UtcDateTime,
            Expires = now.AddSeconds(AccessTokenLifetimeSeconds).UtcDateTime,
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256),
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(descriptor);
        var accessToken = handler.WriteToken(token);

        var refreshToken = GenerateRefreshToken(user.Id, now.AddDays(RefreshTokenLifetimeDays));

        return (accessToken, refreshToken, AccessTokenLifetimeSeconds);
    }

    public Guid? ValidateRefreshToken(string refreshToken)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(refreshToken, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = false,
                ValidateLifetime = true,
                IssuerSigningKey = _signingKey,
                ClockSkew = TimeSpan.Zero,
            }, out _);

            var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return sub is not null && Guid.TryParse(sub, out var id) ? id : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private string GenerateRefreshToken(Guid userId, DateTimeOffset expires)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim("typ", "refresh"),
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires.UtcDateTime,
            Issuer = _issuer,
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256),
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(descriptor);
        return handler.WriteToken(token);
    }
}
