using Microclimate.Domain.Enums;
using Microclimate.Domain.Metrics;

namespace Microclimate.Domain.Tests.Metrics;

public class MetricsTests
{
    public static TheoryData<MetricType> AllMetrics => [.. Enum.GetValues<MetricType>()];

    public static TheoryData<RoomType> AllRoomTypes => [.. Enum.GetValues<RoomType>()];

    [Theory]
    [MemberData(nameof(AllMetrics))]
    public void MetricLimits_EveryMetric_HasRangeAndUnit(MetricType metric)
    {
        var range = MetricLimits.For(metric);

        Assert.True(range.Min < range.Max);
        Assert.False(string.IsNullOrWhiteSpace(MetricLimits.Unit(metric)));
    }

    [Theory]
    [InlineData(MetricType.Humidity, 0, 100)]
    [InlineData(MetricType.Temperature, -40, 85)]
    [InlineData(MetricType.Co2, 0, 10_000)]
    public void MetricLimits_KnownMetric_ReturnsPhysicalBounds(MetricType metric, double min, double max)
    {
        var range = MetricLimits.For(metric);

        Assert.Equal(min, range.Min);
        Assert.Equal(max, range.Max);
    }

    [Fact]
    public void MetricLimits_UndefinedMetric_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MetricLimits.For((MetricType)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => MetricLimits.Unit((MetricType)99));
    }

    [Theory]
    [MemberData(nameof(AllRoomTypes))]
    public void DefaultThresholds_EveryRoomTypeAndMetric_IsDefined(RoomType roomType)
    {
        foreach (var metric in Enum.GetValues<MetricType>())
        {
            Assert.NotNull(DefaultThresholds.For(roomType, metric));
        }
    }

    [Theory]
    [InlineData(RoomType.Office, 26.5, AlertLevel.Warning)]
    [InlineData(RoomType.ServerRoom, 26.5, null)]
    [InlineData(RoomType.ServerRoom, 33, AlertLevel.Critical)]
    public void DefaultThresholds_Temperature_DependsOnRoomType(RoomType roomType, double value, AlertLevel? expected)
    {
        Assert.Equal(expected, DefaultThresholds.For(roomType, MetricType.Temperature).Evaluate(value));
    }

    [Fact]
    public void DefaultThresholds_UndefinedMetric_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DefaultThresholds.For(RoomType.Office, (MetricType)99));
    }
}
