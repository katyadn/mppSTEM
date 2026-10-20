using System.Security.Cryptography;
using System.Text;

using Microclimate.Application.Abstractions.Persistence;
using Microclimate.Application.Analytics;
using Microclimate.Application.Common;
using Microclimate.Domain.Alerts;
using Microclimate.Domain.Enums;
using Microclimate.Domain.Measurements;
using Microclimate.Domain.Rooms;
using Microclimate.Domain.Sensors;
using Microclimate.Domain.Thresholds;
using Microclimate.Domain.Users;
using Microclimate.Domain.ValueObjects;

namespace Microclimate.Api.Storage;

/// <summary>
/// Потокобезпечне in-memory сховище для роботи API до підключення повної EF Core інфраструктури.
/// Дозволяє тестувати всі контролери, автентифікацію та валідацію автономно.
/// </summary>
public sealed class InMemoryDataStore
{
    public List<User> Users { get; } = [];
    public List<Room> Rooms { get; } = [];
    public List<Sensor> Sensors { get; } = [];
    public List<Threshold> Thresholds { get; } = [];
    public List<Alert> Alerts { get; } = [];
    public List<Measurement> Measurements { get; } = [];

    public InMemoryDataStore(TimeProvider time)
    {
        var now = time.GetUtcNow();

        // Створюємо базового адміністратора для тестів і перевірки працездатності:
        // email: admin@microclimate.local, password: AdminPassword123!
        var passwordHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("AdminPassword123!")));
        var admin = User.Create(
            "admin@microclimate.local",
            "Головний Адміністратор",
            passwordHash,
            UserRole.Admin,
            now);
        Users.Add(admin);

        // Оператор: email: operator@microclimate.local, password: OperatorPassword123!
        var operatorHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("OperatorPassword123!")));
        var op = User.Create(
            "operator@microclimate.local",
            "Черговий Оператор",
            operatorHash,
            UserRole.Operator,
            now);
        Users.Add(op);

        // Базова тестова кімната та датчик
        var room = Room.Create("Лабораторія 101", 1, 45.5m, RoomType.Office, now);
        Rooms.Add(room);

        // Датчик з тестовим ключем "test-device-key-12345"
        var deviceKey = "test-device-key-12345";
        var deviceKeyHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(deviceKey)));
        var sensor = Sensor.Create(room.Id, "SN-LAB-101-01", "DHT22-Pro", deviceKeyHash, now);
        Sensors.Add(sensor);
    }
}

public sealed class InMemoryUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);
        await action(cancellationToken);
    }
}

public sealed class InMemoryUserRepository(InMemoryDataStore store) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        lock (store.Users)
        {
            return Task.FromResult(store.Users.Find(u => u.Id == id));
        }
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        lock (store.Users)
        {
            var normalized = email.Trim().ToLowerInvariant();
            return Task.FromResult(store.Users.Find(u => u.Email == normalized));
        }
    }

    public Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken)
    {
        lock (store.Users)
        {
            return Task.FromResult<IReadOnlyList<User>>(store.Users.ToList());
        }
    }

    public void Add(User user)
    {
        lock (store.Users)
        {
            store.Users.Add(user);
        }
    }

    public void Remove(User user)
    {
        lock (store.Users)
        {
            store.Users.Remove(user);
        }
    }
}

public sealed class InMemorySensorRepository(InMemoryDataStore store) : ISensorRepository
{
    public Task<Sensor?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        lock (store.Sensors)
        {
            return Task.FromResult(store.Sensors.Find(s => s.Id == id));
        }
    }

    public Task<Sensor?> GetByDeviceKeyHashAsync(string deviceKeyHash, CancellationToken cancellationToken)
    {
        lock (store.Sensors)
        {
            return Task.FromResult(store.Sensors.Find(s => s.DeviceKeyHash == deviceKeyHash));
        }
    }

    public Task<bool> ExistsBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken)
    {
        lock (store.Sensors)
        {
            return Task.FromResult(store.Sensors.Exists(s => s.SerialNumber.Equals(serialNumber, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public Task<IReadOnlyList<Sensor>> ListByRoomAsync(Guid roomId, CancellationToken cancellationToken)
    {
        lock (store.Sensors)
        {
            return Task.FromResult<IReadOnlyList<Sensor>>(store.Sensors.Where(s => s.RoomId == roomId).ToList());
        }
    }

    public void Add(Sensor sensor)
    {
        lock (store.Sensors)
        {
            store.Sensors.Add(sensor);
        }
    }
}

public sealed class InMemoryRoomRepository(InMemoryDataStore store) : IRoomRepository
{
    public Task<Room?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        lock (store.Rooms)
        {
            return Task.FromResult(store.Rooms.Find(r => r.Id == id));
        }
    }

    public Task<PagedResult<Room>> ListAsync(PageRequest page, CancellationToken cancellationToken)
    {
        lock (store.Rooms)
        {
            var total = store.Rooms.Count;
            var items = store.Rooms
                .Skip(page.Skip)
                .Take(page.PageSize)
                .ToList();

            return Task.FromResult(new PagedResult<Room>(items, page.Page, page.PageSize, total));
        }
    }

    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken)
    {
        lock (store.Rooms)
        {
            return Task.FromResult(store.Rooms.Exists(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public Task<bool> HasSensorsAsync(Guid roomId, CancellationToken cancellationToken)
    {
        lock (store.Sensors)
        {
            return Task.FromResult(store.Sensors.Exists(s => s.RoomId == roomId));
        }
    }

    public void Add(Room room)
    {
        lock (store.Rooms)
        {
            store.Rooms.Add(room);
        }
    }

    public void Remove(Room room)
    {
        lock (store.Rooms)
        {
            store.Rooms.Remove(room);
        }
    }
}

public sealed class InMemoryThresholdRepository(InMemoryDataStore store) : IThresholdRepository
{
    public Task<IReadOnlyList<Threshold>> ListByRoomAsync(Guid roomId, CancellationToken cancellationToken)
    {
        lock (store.Thresholds)
        {
            return Task.FromResult<IReadOnlyList<Threshold>>(store.Thresholds.Where(t => t.RoomId == roomId).ToList());
        }
    }

    public Task<Threshold?> GetAsync(Guid roomId, MetricType metric, CancellationToken cancellationToken)
    {
        lock (store.Thresholds)
        {
            return Task.FromResult(store.Thresholds.Find(t => t.RoomId == roomId && t.Metric == metric));
        }
    }

    public void Add(Threshold threshold)
    {
        lock (store.Thresholds)
        {
            store.Thresholds.Add(threshold);
        }
    }
}

public sealed class InMemoryAlertRepository(InMemoryDataStore store) : IAlertRepository
{
    public Task<Alert?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        lock (store.Alerts)
        {
            return Task.FromResult(store.Alerts.Find(a => a.Id == id));
        }
    }

    public Task<Alert?> GetUnresolvedAsync(Guid roomId, MetricType metric, CancellationToken cancellationToken)
    {
        lock (store.Alerts)
        {
            return Task.FromResult(store.Alerts.Find(a => a.RoomId == roomId && a.Metric == metric && a.Status != AlertStatus.Resolved));
        }
    }

    public Task<PagedResult<Alert>> ListAsync(AlertFilter filter, PageRequest page, CancellationToken cancellationToken)
    {
        lock (store.Alerts)
        {
            var query = store.Alerts.AsEnumerable();
            if (filter.RoomId.HasValue)
            {
                query = query.Where(a => a.RoomId == filter.RoomId.Value);
            }

            if (filter.Level.HasValue)
            {
                query = query.Where(a => a.Level == filter.Level.Value);
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(a => a.Status == filter.Status.Value);
            }

            var list = query.ToList();
            var total = list.Count;
            var items = list.Skip(page.Skip).Take(page.PageSize).ToList();

            return Task.FromResult(new PagedResult<Alert>(items, page.Page, page.PageSize, total));
        }
    }

    public void Add(Alert alert)
    {
        lock (store.Alerts)
        {
            store.Alerts.Add(alert);
        }
    }
}

public sealed class InMemoryMeasurementRepository(InMemoryDataStore store) : IMeasurementRepository
{
    public Task AddRangeAsync(IReadOnlyCollection<Measurement> measurements, CancellationToken cancellationToken)
    {
        lock (store.Measurements)
        {
            store.Measurements.AddRange(measurements);
        }

        return Task.CompletedTask;
    }

    public Task<PagedResult<Measurement>> GetByRoomAsync(
        Guid roomId,
        MetricType metric,
        DateRange range,
        PageRequest page,
        CancellationToken cancellationToken)
    {
        lock (store.Measurements)
        {
            var sensorIds = store.Sensors.Where(s => s.RoomId == roomId).Select(s => s.Id).ToHashSet();
            var query = store.Measurements
                .Where(m => sensorIds.Contains(m.SensorId) && m.Metric == metric && range.Contains(m.MeasuredAt))
                .OrderByDescending(m => m.MeasuredAt)
                .ToList();

            var total = query.Count;
            var items = query.Skip(page.Skip).Take(page.PageSize).ToList();

            return Task.FromResult(new PagedResult<Measurement>(items, page.Page, page.PageSize, total));
        }
    }

    public async IAsyncEnumerable<Measurement> StreamByRoomAsync(
        Guid roomId,
        MetricType? metric,
        DateRange range,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        List<Measurement> snapshot;
        lock (store.Measurements)
        {
            var sensorIds = store.Sensors.Where(s => s.RoomId == roomId).Select(s => s.Id).ToHashSet();
            snapshot = store.Measurements
                .Where(m => sensorIds.Contains(m.SensorId) && (!metric.HasValue || m.Metric == metric.Value) && range.Contains(m.MeasuredAt))
                .OrderBy(m => m.MeasuredAt)
                .ToList();
        }

        foreach (var m in snapshot)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return m;
            await Task.Yield();
        }
    }

    public Task<IReadOnlyList<MetricSummary>> GetSummaryAsync(
        Guid roomId,
        MetricType? metric,
        DateRange range,
        CancellationToken cancellationToken)
    {
        lock (store.Measurements)
        {
            var sensorIds = store.Sensors.Where(s => s.RoomId == roomId).Select(s => s.Id).ToHashSet();
            var query = store.Measurements
                .Where(m => sensorIds.Contains(m.SensorId) && (!metric.HasValue || m.Metric == metric.Value) && range.Contains(m.MeasuredAt))
                .GroupBy(m => m.Metric)
                .Select(g => new MetricSummary(
                    g.Key,
                    g.Count(),
                    g.Min(x => (double?)x.Value),
                    g.Max(x => (double?)x.Value),
                    g.Average(x => x.Value)))
                .ToList();

            return Task.FromResult<IReadOnlyList<MetricSummary>>(query);
        }
    }

    public Task<IReadOnlyList<SeriesPoint>> GetSeriesAsync(
        Guid roomId,
        MetricType metric,
        DateRange range,
        AggregationInterval interval,
        CancellationToken cancellationToken)
    {
        lock (store.Measurements)
        {
            var sensorIds = store.Sensors.Where(s => s.RoomId == roomId).Select(s => s.Id).ToHashSet();
            var points = store.Measurements
                .Where(m => sensorIds.Contains(m.SensorId) && m.Metric == metric && range.Contains(m.MeasuredAt))
                .OrderBy(m => m.MeasuredAt)
                .Select(m => new SeriesPoint(m.MeasuredAt, 1, m.Value, m.Value, m.Value))
                .ToList();

            return Task.FromResult<IReadOnlyList<SeriesPoint>>(points);
        }
    }

    public Task<IReadOnlyList<Measurement>> GetLatestByRoomAsync(Guid roomId, CancellationToken cancellationToken)
    {
        lock (store.Measurements)
        {
            var sensorIds = store.Sensors.Where(s => s.RoomId == roomId).Select(s => s.Id).ToHashSet();
            var latest = store.Measurements
                .Where(m => sensorIds.Contains(m.SensorId))
                .GroupBy(m => m.Metric)
                .Select(g => g.OrderByDescending(m => m.MeasuredAt).First())
                .ToList();

            return Task.FromResult<IReadOnlyList<Measurement>>(latest);
        }
    }
}
