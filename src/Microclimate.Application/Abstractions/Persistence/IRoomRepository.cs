using Microclimate.Application.Common;
using Microclimate.Domain.Rooms;

namespace Microclimate.Application.Abstractions.Persistence;

/// <summary>Доступ до кімнат.</summary>
public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Кімнати, впорядковані за поверхом і назвою.</summary>
    Task<PagedResult<Room>> ListAsync(PageRequest page, CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);

    /// <summary>Чи є в кімнаті датчики (тоді видалення заборонено).</summary>
    Task<bool> HasSensorsAsync(Guid roomId, CancellationToken cancellationToken);

    void Add(Room room);

    void Remove(Room room);
}
