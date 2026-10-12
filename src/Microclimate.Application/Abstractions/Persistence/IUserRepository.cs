using Microclimate.Domain.Users;

namespace Microclimate.Application.Abstractions.Persistence;

/// <summary>Доступ до користувачів (сценарії автентифікації — Учасник 2).</summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Пошук за email; email нормалізується так само, як у <see cref="User.NormalizeEmail"/>.</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken);

    void Add(User user);

    void Remove(User user);
}
