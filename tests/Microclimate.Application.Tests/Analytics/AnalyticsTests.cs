using Microclimate.Application.Abstractions.Persistence;
using Microclimate.Application.Analytics;
using Microclimate.Application.Common;
using Microclimate.Domain.Enums;
using Microclimate.Domain.Measurements;
using Microclimate.Domain.Metrics;
using Microclimate.Domain.Rooms;
using Microclimate.Domain.Thresholds;
using Microclimate.Domain.ValueObjects;

namespace Microclimate.Application.Tests.Analytics;

public class AnalyticsTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    private static ComfortInput Office(MetricType metric, double value) =>
        new(metric, value, DefaultThresholds.For(RoomType.Office, metric));

    [Fact]
    public void Calculate_ExampleFromSpecification_Returns84_55Acceptable()
    {
        var result = new ComfortIndexCalculator().Calculate(
        [
            Office(MetricType.Temperature, 25.5),
            Office(MetricType.Humidity, 35),
            Office(MetricType.Co2, 1300),
            Office(MetricType.Pm25, 8),
        ]);

        Assert.Equal(84.55, result.Index);
        Assert.Equal(ComfortCategory.Acceptable, result.Category);
    }

    [Fact]
    public void Calculate_CriticalMetric_IsUncomfortable_AndNoInputsIsNoData()
    {
        var calculator = new ComfortIndexCalculator();

        var critical = calculator.Calculate([Office(MetricType.Temperature, 22), Office(MetricType.Co2, 1800)]);
        var empty = calculator.Calculate([]);

        Assert.Equal(ComfortCategory.Uncomfortable, critical.Category);
        Assert.Null(empty.Index);
        Assert.Equal(ComfortCategory.NoData, empty.Category);
    }

    [Fact]
    public async Task GetSeries_HourWithoutData_ReturnsEmptyBucket()
    {
        var room = Room.Create("101", 1, null, RoomType.Office, Now);
        var measurements = new FakeMeasurements([new SeriesPoint(Now, 2, 22, 21, 23), new SeriesPoint(Now.AddHours(2), 1, 24, 24, 24)]);
        var service = new AnalyticsService(new FakeRooms(room), measurements, new FakeThresholds(), new ComfortIndexCalculator(), TimeProvider.System);

        var series = await service.GetSeriesAsync(room.Id, MetricType.Temperature, DateRange.Create(Now, Now.AddHours(3)), AggregationInterval.Hour, CancellationToken.None);

        Assert.Equal([2, 0, 1], series.Select(p => p.Count));
        Assert.Null(series[1].Avg);
    }

    private sealed class FakeRooms(Room room) : IRoomRepository
    {
        public Task<Room?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Room?>(id == room.Id ? room : null);

        public Task<PagedResult<Room>> ListAsync(PageRequest page, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<bool> HasSensorsAsync(Guid roomId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public void Add(Room room) => throw new NotSupportedException();

        public void Remove(Room room) => throw new NotSupportedException();
    }

    private sealed class FakeThresholds : IThresholdRepository
    {
        public Task<IReadOnlyList<Threshold>> ListByRoomAsync(Guid roomId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Threshold>>([]);

        public Task<Threshold?> GetAsync(Guid roomId, MetricType metric, CancellationToken cancellationToken) => Task.FromResult<Threshold?>(null);

        public void Add(Threshold threshold) => throw new NotSupportedException();
    }

    private sealed class FakeMeasurements(IReadOnlyList<SeriesPoint> series) : IMeasurementRepository
    {
        public Task<IReadOnlyList<SeriesPoint>> GetSeriesAsync(Guid roomId, MetricType metric, DateRange range, AggregationInterval interval, CancellationToken cancellationToken) => Task.FromResult(series);

        public Task<IReadOnlyList<MetricSummary>> GetSummaryAsync(Guid roomId, MetricType? metric, DateRange range, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<MetricSummary>>([]);

        public Task AddRangeAsync(IReadOnlyCollection<Measurement> measurements, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<PagedResult<Measurement>> GetByRoomAsync(Guid roomId, MetricType metric, DateRange range, PageRequest page, CancellationToken cancellationToken) => throw new NotSupportedException();

        public IAsyncEnumerable<Measurement> StreamByRoomAsync(Guid roomId, MetricType? metric, DateRange range, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<Measurement>> GetLatestByRoomAsync(Guid roomId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
