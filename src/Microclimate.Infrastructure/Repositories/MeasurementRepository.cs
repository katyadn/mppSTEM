using System.Runtime.CompilerServices;

using Microclimate.Application.Abstractions.Persistence;
using Microclimate.Application.Analytics;
using Microclimate.Application.Common;
using Microclimate.Domain.Enums;
using Microclimate.Domain.Measurements;
using Microclimate.Domain.ValueObjects;
using Microclimate.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Microclimate.Infrastructure.Repositories;

public sealed class MeasurementRepository(AppDbContext db) : IMeasurementRepository
{
    public Task AddRangeAsync(IReadOnlyCollection<Measurement> measurements, CancellationToken cancellationToken) =>
        db.Measurements.AddRangeAsync(measurements, cancellationToken);

    public Task<PagedResult<Measurement>> GetByRoomAsync(Guid roomId, MetricType metric, DateRange range, PageRequest page, CancellationToken cancellationToken) =>
        InRoom(roomId, metric, range).OrderBy(m => m.MeasuredAt).ToPagedAsync(page, cancellationToken);

    public async IAsyncEnumerable<Measurement> StreamByRoomAsync(
        Guid roomId,
        MetricType? metric,
        DateRange range,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var query = InRoom(roomId, metric, range).OrderBy(m => m.MeasuredAt).AsNoTracking().AsAsyncEnumerable();
        await foreach (var measurement in query.WithCancellation(cancellationToken))
        {
            yield return measurement;
        }
    }

    public async Task<IReadOnlyList<MetricSummary>> GetSummaryAsync(Guid roomId, MetricType? metric, DateRange range, CancellationToken cancellationToken)
    {
        // Агрегати рахуються в БД (GROUP BY), у пам'ять приходить один рядок на метрику.
        var rows = await InRoom(roomId, metric, range)
            .GroupBy(m => m.Metric)
            .Select(g => new
            {
                Metric = g.Key,
                Count = g.Count(),
                Min = g.Min(m => m.Value),
                Max = g.Max(m => m.Value),
                Avg = g.Average(m => m.Value),
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new MetricSummary(r.Metric, r.Count, r.Min, r.Max, r.Avg)).ToList();
    }

    public async Task<IReadOnlyList<SeriesPoint>> GetSeriesAsync(Guid roomId, MetricType metric, DateRange range, AggregationInterval interval, CancellationToken cancellationToken)
    {
        // Обрізання часу до години чи доби SQLite і PostgreSQL виконують по-різному,
        // тому з БД читаються лише дві колонки, а групування робиться тут (DECISIONS.md, D-1-012).
        var values = await InRoom(roomId, metric, range)
            .OrderBy(m => m.MeasuredAt)
            .Select(m => new { m.MeasuredAt, m.Value })
            .ToListAsync(cancellationToken);

        return values
            .GroupBy(v => AggregationBuckets.Floor(v.MeasuredAt, interval))
            .Select(g => new SeriesPoint(g.Key, g.Count(), g.Average(v => v.Value), g.Min(v => v.Value), g.Max(v => v.Value)))
            .ToList();
    }

    public async Task<IReadOnlyList<Measurement>> GetLatestByRoomAsync(Guid roomId, CancellationToken cancellationToken)
    {
        // Один запит: для кожної метрики — вимір з найпізнішим часом.
        var measurements = RoomMeasurements(roomId);
        var latestTimes = measurements.GroupBy(m => m.Metric).Select(g => new { Metric = g.Key, MeasuredAt = g.Max(m => m.MeasuredAt) });

        var rows = await measurements
            .Where(m => latestTimes.Any(l => l.Metric == m.Metric && l.MeasuredAt == m.MeasuredAt))
            .ToListAsync(cancellationToken);

        // Якщо два датчики кімнати надіслали вимір в одну мить, береться один.
        return rows.GroupBy(m => m.Metric).Select(g => g.MaxBy(m => m.Id)!).ToList();
    }

    // Виміри кімнати — це виміри її датчиків.
    private IQueryable<Measurement> RoomMeasurements(Guid roomId)
    {
        var sensorIds = db.Sensors.Where(s => s.RoomId == roomId).Select(s => s.Id);
        return db.Measurements.Where(m => sensorIds.Contains(m.SensorId));
    }

    private IQueryable<Measurement> InRoom(Guid roomId, MetricType? metric, DateRange range)
    {
        ArgumentNullException.ThrowIfNull(range);

        var query = RoomMeasurements(roomId).Where(m => m.MeasuredAt >= range.From && m.MeasuredAt < range.To);
        return metric is { } single ? query.Where(m => m.Metric == single) : query;
    }
}
