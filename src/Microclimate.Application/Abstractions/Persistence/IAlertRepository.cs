using Microclimate.Application.Common;
using Microclimate.Domain.Alerts;
using Microclimate.Domain.Enums;

namespace Microclimate.Application.Abstractions.Persistence;

/// <summary>Фільтр стрічки сповіщень.</summary>
public sealed record AlertFilter(Guid? RoomId = null, AlertLevel? Level = null, AlertStatus? Status = null);

/// <summary>Доступ до сповіщень.</summary>
public interface IAlertRepository
{
    Task<Alert?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Незакрите (Open або Acknowledged) сповіщення метрики кімнати, якщо є.</summary>
    Task<Alert?> GetUnresolvedAsync(Guid roomId, MetricType metric, CancellationToken cancellationToken);

    /// <summary>Стрічка сповіщень, новіші спочатку.</summary>
    Task<PagedResult<Alert>> ListAsync(AlertFilter filter, PageRequest page, CancellationToken cancellationToken);

    void Add(Alert alert);
}
