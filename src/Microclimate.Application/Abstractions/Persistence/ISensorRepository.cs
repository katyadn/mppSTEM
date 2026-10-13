using Microclimate.Domain.Sensors;

namespace Microclimate.Application.Abstractions.Persistence;

/// <summary>Доступ до датчиків.</summary>
public interface ISensorRepository
{
    Task<Sensor?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken);

    Task<IReadOnlyList<Sensor>> ListByRoomAsync(Guid roomId, CancellationToken cancellationToken);

    void Add(Sensor sensor);
}
