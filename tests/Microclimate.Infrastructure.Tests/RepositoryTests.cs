using Microclimate.Application.Analytics;
using Microclimate.Application.Common;
using Microclimate.Domain.Alerts;
using Microclimate.Domain.Enums;
using Microclimate.Domain.Measurements;
using Microclimate.Domain.Rooms;
using Microclimate.Domain.Sensors;
using Microclimate.Domain.ValueObjects;
using Microclimate.Infrastructure.Persistence;
using Microclimate.Infrastructure.Repositories;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Microclimate.Infrastructure.Tests;

/// <summary>Репозиторії на SQLite в пам'яті зі справжніми міграціями.</summary>
public sealed class RepositoryTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly SqliteAppDbContext _db;

    public RepositoryTests()
    {
        _connection.Open();
        _db = new SqliteAppDbContext(new DbContextOptionsBuilder<SqliteAppDbContext>().UseSqlite(_connection).Options);
        _db.Database.Migrate();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Rooms_ListAsync_PagesOrderedByFloor()
    {
        var rooms = new RoomRepository(_db);
        rooms.Add(Room.Create("201", 2, null, RoomType.Office, Now));
        rooms.Add(Room.Create("101", 1, 20m, RoomType.Classroom, Now));
        await _db.SaveChangesAsync();

        var page = await rooms.ListAsync(new PageRequest(1, 1), CancellationToken.None);

        Assert.Equal(2, page.TotalCount);
        Assert.Equal("101", Assert.Single(page.Items).Name);
        Assert.True(await rooms.ExistsByNameAsync("201", CancellationToken.None));
    }

    [Fact]
    public async Task Measurements_SummaryAndSeries_AggregateByRoom()
    {
        var sensor = await SeedSensorAsync();
        var repository = new MeasurementRepository(_db);
        await repository.AddRangeAsync(
        [
            Measurement.Record(sensor, MetricType.Temperature, 21, Now, Now.AddHours(2)),
            Measurement.Record(sensor, MetricType.Temperature, 23, Now.AddMinutes(30), Now.AddHours(2)),
            Measurement.Record(sensor, MetricType.Temperature, 25, Now.AddHours(1), Now.AddHours(2)),
            Measurement.Record(sensor, MetricType.Co2, 800, Now, Now.AddHours(2)),
        ], CancellationToken.None);
        await _db.SaveChangesAsync();
        var range = DateRange.Create(Now, Now.AddHours(2));

        var summary = await repository.GetSummaryAsync(sensor.RoomId, MetricType.Temperature, range, CancellationToken.None);
        var series = await repository.GetSeriesAsync(sensor.RoomId, MetricType.Temperature, range, AggregationInterval.Hour, CancellationToken.None);

        var temperature = Assert.Single(summary);
        Assert.Equal(3, temperature.Count);
        Assert.Equal(23, temperature.Avg);
        Assert.Equal([2, 1], series.Select(p => p.Count));
        Assert.Equal(22, series[0].Avg);
    }

    [Fact]
    public async Task Alerts_GetUnresolved_IgnoresResolvedAlerts()
    {
        var sensor = await SeedSensorAsync();
        var repository = new AlertRepository(_db);
        var old = Alert.Raise(sensor.RoomId, sensor.Id, MetricType.Co2, AlertLevel.Warning, 1300, 1200, "CO2", Now);
        old.Resolve(Now.AddMinutes(5));
        var open = Alert.Raise(sensor.RoomId, sensor.Id, MetricType.Co2, AlertLevel.Critical, 1800, 1750, "CO2", Now.AddMinutes(10));
        repository.Add(old);
        repository.Add(open);
        await _db.SaveChangesAsync();

        var unresolved = await repository.GetUnresolvedAsync(sensor.RoomId, MetricType.Co2, CancellationToken.None);

        Assert.Equal(open.Id, unresolved?.Id);
    }

    [Fact]
    public async Task ExecuteInTransaction_FailedAction_SavesNothing()
    {
        var rooms = new RoomRepository(_db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _db.ExecuteInTransactionAsync(
            async ct =>
            {
                // Зміни вже записані в БД, але транзакцію не підтверджено.
                rooms.Add(Room.Create("301", 3, null, RoomType.Office, Now));
                await _db.SaveChangesAsync(ct);
                throw new InvalidOperationException("Помилка під час обробки.");
            },
            CancellationToken.None));
        _db.ChangeTracker.Clear();

        Assert.False(await rooms.ExistsByNameAsync("301", CancellationToken.None));
    }

    private async Task<Sensor> SeedSensorAsync()
    {
        var room = Room.Create("101", 1, null, RoomType.Office, Now);
        var sensor = Sensor.Create(room.Id, "SN-001", null, "hash", Now);
        _db.Rooms.Add(room);
        _db.Sensors.Add(sensor);
        await _db.SaveChangesAsync();
        return sensor;
    }
}
