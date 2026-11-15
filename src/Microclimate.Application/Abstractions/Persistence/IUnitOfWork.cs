namespace Microclimate.Application.Abstractions.Persistence;

/// <summary>Фіксація змін і транзакції.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Виконує дію в одній транзакції БД: або всі зміни зберігаються, або
    /// жодна.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action,
        CancellationToken cancellationToken);
}
