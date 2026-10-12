using Microclimate.Domain.Buildings;

namespace Microclimate.Application.Abstractions.Persistence;

/// <summary>Доступ до будівель.</summary>
public interface IBuildingRepository
{
    Task<Building?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Building>> ListAsync(CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);

    /// <summary>Чи має будівля кімнати (видалення заборонено).</summary>
    Task<bool> HasRoomsAsync(Guid buildingId, CancellationToken cancellationToken);

    void Add(Building building);

    void Remove(Building building);
}
