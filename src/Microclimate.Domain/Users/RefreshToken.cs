using Microclimate.Domain.Common;

namespace Microclimate.Domain.Users;

/// <summary>Refresh-токен користувача. Зберігається лише хеш; токени ротуються при кожному оновленні.</summary>
public sealed class RefreshToken : Entity<Guid>
{
    public const int TokenHashMaxLength = 128;

    // Для EF Core
    private RefreshToken()
    {
        TokenHash = string.Empty;
    }

    private RefreshToken(Guid id, Guid userId, string tokenHash, DateTimeOffset createdAt, DateTimeOffset expiresAt)
    {
        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>Хеш токена, який замінив цей під час ротації.</summary>
    public string? ReplacedByTokenHash { get; private set; }

    public static RefreshToken Issue(Guid userId, string tokenHash, DateTimeOffset now, TimeSpan lifetime)
    {
        if (lifetime <= TimeSpan.Zero)
        {
            throw new DomainException("Термін дії refresh-токена має бути додатним.");
        }

        var createdAt = now.ToUniversalTime();
        return new RefreshToken(
            Guid.CreateVersion7(now),
            Guard.NotEmpty(userId, "Користувач"),
            Guard.RequiredText(tokenHash, "Хеш токена", TokenHashMaxLength),
            createdAt,
            createdAt + lifetime);
    }

    /// <summary>Токен можна використати: не відкликаний і не прострочений.</summary>
    public bool IsActive(DateTimeOffset now) => RevokedAt is null && now.ToUniversalTime() < ExpiresAt;

    /// <summary>Відкликає токен; при ротації вказується хеш нового токена.</summary>
    public void Revoke(DateTimeOffset now, string? replacedByTokenHash = null)
    {
        if (RevokedAt is not null)
        {
            throw new DomainException("Refresh-токен уже відкликано.");
        }

        RevokedAt = now.ToUniversalTime();
        ReplacedByTokenHash = Guard.OptionalText(replacedByTokenHash, "Хеш нового токена", TokenHashMaxLength);
    }
}
