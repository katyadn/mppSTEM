using System.Net.Mail;

using Microclimate.Domain.Common;
using Microclimate.Domain.Enums;

namespace Microclimate.Domain.Users;

/// <summary>Користувач системи.</summary>
public sealed class User : Entity<Guid>
{
    public const int EmailMaxLength = 256;
    public const int DisplayNameMaxLength = 100;
    public const int PasswordHashMaxLength = 256;

    // Для EF Core
    private User()
    {
        Email = string.Empty;
        DisplayName = string.Empty;
        PasswordHash = string.Empty;
    }

    private User(Guid id, string email, string displayName, string passwordHash, UserRole role, DateTimeOffset createdAt)
    {
        Id = id;
        Email = email;
        DisplayName = displayName;
        PasswordHash = passwordHash;
        Role = role;
        CreatedAt = createdAt;
        IsActive = true;
    }

    /// <summary>Email у нижньому регістрі.</summary>
    public string Email { get; private set; }

    public string DisplayName { get; private set; }

    /// <summary>Хеш пароля (обчислює шар Api); сам пароль не зберігається.</summary>
    public string PasswordHash { get; private set; }

    public UserRole Role { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static User Create(string email, string displayName, string passwordHash, UserRole role, DateTimeOffset now) =>
        new(
            Guid.CreateVersion7(now),
            NormalizeEmail(email),
            Guard.RequiredText(displayName, "Ім'я", DisplayNameMaxLength),
            Guard.RequiredText(passwordHash, "Хеш пароля", PasswordHashMaxLength),
            Guard.Defined(role, "Роль"),
            now.ToUniversalTime());

    /// <summary>Канонічний вигляд email без перевірки формату: без пробілів по краях, у нижньому регістрі.</summary>
    public static string CanonicalEmail(string email)
    {
        ArgumentNullException.ThrowIfNull(email);
        return email.Trim().ToLowerInvariant();
    }

    /// <summary>Канонічний вигляд email з перевіркою, що він заданий, не задовгий і має правильний формат.</summary>
    public static string NormalizeEmail(string email)
    {
        var normalized = CanonicalEmail(Guard.RequiredText(email, "Email", EmailMaxLength));
        if (!MailAddress.TryCreate(normalized, out var address) || address.Address != normalized)
        {
            throw new DomainException($"Email '{normalized}' має некоректний формат.");
        }

        return normalized;
    }

    public void ChangeRole(UserRole role) => Role = Guard.Defined(role, "Роль");

    public void Deactivate() => IsActive = false;
}
