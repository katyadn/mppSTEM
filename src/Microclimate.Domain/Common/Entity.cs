namespace Microclimate.Domain.Common;

/// <summary>Базовий клас сутності з ідентифікатором.</summary>
/// <typeparam name="TId">Тип ідентифікатора.</typeparam>
public abstract class Entity<TId>
    where TId : notnull
{
    /// <summary>Ідентифікатор сутності.</summary>
    public TId Id { get; protected set; } = default!;
}
