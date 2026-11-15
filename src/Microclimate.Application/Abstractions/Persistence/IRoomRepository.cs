using Microclimate.Application.Common;
using Microclimate.Domain.Rooms;

namespace Microclimate.Application.Abstractions.Persistence;

/// <summary>Доступ до кімнат.</summary>
public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<Room>> ListAsync(PageRequest page, CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);

    Task<bool> HasSensorsAsync(Guid roomId, CancellationToken cancellationToken);

    void Add(Room room);

    void Remove(Room room);
}
