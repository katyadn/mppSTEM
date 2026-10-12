using Microclimate.Domain.Enums;
using Microclimate.Domain.Thresholds;

namespace Microclimate.Application.Abstractions.Persistence;

/// <summary>Доступ до порогів кімнат. Якщо порогу немає, сервіси використовують DefaultThresholds.</summary>
public interface IThresholdRepository
{
    Task<IReadOnlyList<Threshold>> ListByRoomAsync(Guid roomId, CancellationToken cancellationToken);

    Task<Threshold?> GetAsync(Guid roomId, MetricType metric, CancellationToken cancellationToken);

    void Add(Threshold threshold);
}
