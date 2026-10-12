using Microclimate.Application.Common;
using Microclimate.Domain.Rooms;

namespace Microclimate.Application.Abstractions.Persistence;

/// <summary>Доступ до кімнат.</summary>
public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Кімнати (усі або однієї будівлі), впорядковані за будівлею й назвою.</summary>
    Task<PagedResult<Room>> ListAsync(Guid? buildingId, PageRequest page, CancellationToken cancellationToken);

    /// <summary>Чи є в будівлі кімната з такою назвою (унікальність у межах будівлі).</summary>
    Task<bool> ExistsByNameAsync(Guid buildingId, string name, CancellationToken cancellationToken);

    /// <summary>Чи має кімната датчики або виміри (видалення заборонено).</summary>
    Task<bool> HasHistoryAsync(Guid roomId, CancellationToken cancellationToken);

    void Add(Room room);

    void Remove(Room room);
}
