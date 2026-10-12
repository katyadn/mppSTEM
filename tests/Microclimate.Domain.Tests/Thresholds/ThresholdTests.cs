using Microclimate.Domain.Common;
using Microclimate.Domain.Enums;
using Microclimate.Domain.Thresholds;

namespace Microclimate.Domain.Tests.Thresholds;

public class ThresholdTests
{
    [Fact]
    public void Create_ValidLimits_StoresBoundsAndEvaluates()
    {
        // Arrange
        var limits = new ThresholdLimits(null, null, 1000, 1500);

        // Act
        var threshold = Threshold.Create(TestData.RoomId, MetricType.Co2, limits, TestData.Now);

        // Assert
        Assert.Equal(TestData.RoomId, threshold.RoomId);
        Assert.Equal(MetricType.Co2, threshold.Metric);
        Assert.Equal(limits, threshold.Limits);
        Assert.Equal(AlertLevel.Warning, threshold.Evaluate(1100));
        Assert.Null(threshold.Evaluate(900));
    }

    [Fact]
    public void Update_NewLimits_ReplacesAllBounds()
    {
        var threshold = Threshold.Create(TestData.RoomId, MetricType.Temperature, new ThresholdLimits(16, 20, 25, 28), TestData.Now);

        threshold.Update(new ThresholdLimits(null, 18, 27, null));

        Assert.Null(threshold.CriticalMin);
        Assert.Equal(18, threshold.WarningMin);
        Assert.Equal(27, threshold.WarningMax);
        Assert.Null(threshold.CriticalMax);
    }

    [Fact]
    public void Create_EmptyRoom_Throws()
    {
        Assert.Throws<DomainException>(() =>
            Threshold.Create(Guid.Empty, MetricType.Co2, new ThresholdLimits(null, null, 1000, null), TestData.Now));
    }

    [Fact]
    public void Create_UndefinedMetric_Throws()
    {
        Assert.Throws<DomainException>(() =>
            Threshold.Create(TestData.RoomId, (MetricType)99, new ThresholdLimits(null, null, 1000, null), TestData.Now));
    }
}
