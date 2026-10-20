using Microclimate.Application.Abstractions.Persistence;
using Microclimate.Application.Common;
using Microclimate.Domain.Alerts;
using Microclimate.Domain.Enums;
using Microclimate.Domain.Rooms;
using Microclimate.Domain.Sensors;
using Microclimate.Domain.Thresholds;
using Microclimate.Domain.Users;
using Microclimate.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Microclimate.Infrastructure.Repositories;

public sealed class RoomRepository(AppDbContext db) : IRoomRepository
{
    public Task<Room?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Rooms.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<PagedResult<Room>> ListAsync(PageRequest page, CancellationToken cancellationToken) =>
        db.Rooms.OrderBy(r => r.Floor).ThenBy(r => r.Name).ToPagedAsync(page, cancellationToken);

    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken) =>
        db.Rooms.AnyAsync(r => r.Name == name.Trim(), cancellationToken);

    public Task<bool> HasSensorsAsync(Guid roomId, CancellationToken cancellationToken) =>
        db.Sensors.AnyAsync(s => s.RoomId == roomId, cancellationToken);

    public void Add(Room room) => db.Rooms.Add(room);

    public void Remove(Room room) => db.Rooms.Remove(room);
}

public sealed class SensorRepository(AppDbContext db) : ISensorRepository
{
    public Task<Sensor?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Sensors.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    /// <summary>Пошук датчика за хешем ключа X-Device-Key (автентифікація датчиків в Api).</summary>
    public Task<Sensor?> GetByDeviceKeyHashAsync(string deviceKeyHash, CancellationToken cancellationToken) =>
        db.Sensors.FirstOrDefaultAsync(s => s.DeviceKeyHash == deviceKeyHash, cancellationToken);

    public Task<bool> ExistsBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken) =>
        db.Sensors.AnyAsync(s => s.SerialNumber == serialNumber.Trim(), cancellationToken);

    public async Task<IReadOnlyList<Sensor>> ListByRoomAsync(Guid roomId, CancellationToken cancellationToken) =>
        await db.Sensors.Where(s => s.RoomId == roomId).OrderBy(s => s.SerialNumber).ToListAsync(cancellationToken);

    public void Add(Sensor sensor) => db.Sensors.Add(sensor);
}

public sealed class ThresholdRepository(AppDbContext db) : IThresholdRepository
{
    public async Task<IReadOnlyList<Threshold>> ListByRoomAsync(Guid roomId, CancellationToken cancellationToken) =>
        await db.Thresholds.Where(t => t.RoomId == roomId).ToListAsync(cancellationToken);

    public Task<Threshold?> GetAsync(Guid roomId, MetricType metric, CancellationToken cancellationToken) =>
        db.Thresholds.FirstOrDefaultAsync(t => t.RoomId == roomId && t.Metric == metric, cancellationToken);

    public void Add(Threshold threshold) => db.Thresholds.Add(threshold);
}

public sealed class AlertRepository(AppDbContext db) : IAlertRepository
{
    public Task<Alert?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Alerts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<Alert?> GetUnresolvedAsync(Guid roomId, MetricType metric, CancellationToken cancellationToken) =>
        db.Alerts
            .Where(a => a.RoomId == roomId && a.Metric == metric && a.Status != AlertStatus.Resolved)
            .OrderByDescending(a => a.RaisedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<PagedResult<Alert>> ListAsync(AlertFilter filter, PageRequest page, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var query = db.Alerts.AsQueryable();
        if (filter.RoomId is { } roomId)
        {
            query = query.Where(a => a.RoomId == roomId);
        }

        if (filter.Level is { } level)
        {
            query = query.Where(a => a.Level == level);
        }

        if (filter.Status is { } status)
        {
            query = query.Where(a => a.Status == status);
        }

        return query.OrderByDescending(a => a.RaisedAt).ToPagedAsync(page, cancellationToken);
    }

    public void Add(Alert alert) => db.Alerts.Add(alert);
}

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var canonical = User.CanonicalEmail(email);
        return db.Users.FirstOrDefaultAsync(u => u.Email == canonical, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken) =>
        await db.Users.OrderBy(u => u.Email).ToListAsync(cancellationToken);

    public void Add(User user) => db.Users.Add(user);

    public void Remove(User user) => db.Users.Remove(user);
}

internal static class PagingExtensions
{
    public static async Task<PagedResult<T>> ToPagedAsync<T>(this IQueryable<T> query, PageRequest page, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(page.Skip).Take(page.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<T>(items, page.Page, page.PageSize, total);
    }
}
