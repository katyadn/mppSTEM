namespace Microclimate.Application.Abstractions.Persistence;

/// <summary>Фіксація змін і транзакції (реалізація — DbContext в Infrastructure).</summary>
public interface IUnitOfWork
{
    /// <summary>Зберігає зміни, накопичені репозиторіями. Повертає кількість змінених рядків.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Виконує дію в одній транзакції БД: або всі зміни зберігаються, або жодна.
    /// Використовується для збереження пакета вимірів разом зі сповіщеннями (docs/db-schema.md, розділ 4).
    /// </summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken);
}
