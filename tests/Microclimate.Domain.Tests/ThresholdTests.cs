using Microclimate.Domain.Common;
using Microclimate.Domain.Enums;
using Microclimate.Domain.Metrics;
using Microclimate.Domain.Thresholds;

namespace Microclimate.Domain.Tests;

public class ThresholdTests
{
    private static readonly ThresholdLimits Office = new(16, 20, 25, 28);

    [Theory]
    [InlineData(22, null)]
    [InlineData(25, null)]
    [InlineData(25.01, AlertLevel.Warning)]
    [InlineData(19.99, AlertLevel.Warning)]
    [InlineData(28, AlertLevel.Warning)]
    [InlineData(28.01, AlertLevel.Critical)]
    [InlineData(15.99, AlertLevel.Critical)]
    public void Evaluate_OfficeTemperature_ReturnsLevel(double value, AlertLevel? expected)
    {
        Assert.Equal(expected, Office.Evaluate(value));
    }

    [Theory]
    [InlineData(22, null)]
    [InlineData(26, 25.0)]
    [InlineData(29, 28.0)]
    public void ExceededBound_Value_ReturnsViolatedBound(double value, double? expected)
    {
        Assert.Equal(expected, Office.ExceededBound(value));
    }

    [Theory]
    [InlineData(21.0, 20.0, 25.0, 28.0)]
    [InlineData(16.0, 25.0, 25.0, 28.0)]
    [InlineData(30.0, null, null, 28.0)]
    [InlineData(null, null, null, null)]
    public void Constructor_InvalidBounds_Throws(double? criticalMin, double? warningMin, double? warningMax, double? criticalMax)
    {
        Assert.Throws<DomainException>(() => new ThresholdLimits(criticalMin, warningMin, warningMax, criticalMax));
    }

    [Theory]
    [InlineData(RoomType.Office, 26.5, AlertLevel.Warning)]
    [InlineData(RoomType.ServerRoom, 26.5, null)]
    public void DefaultThresholds_Temperature_DependsOnRoomType(RoomType roomType, double value, AlertLevel? expected)
    {
        Assert.Equal(expected, DefaultThresholds.For(roomType, MetricType.Temperature).Evaluate(value));
    }

    [Fact]
    public void Threshold_CreateAndUpdate_UsesNewBounds()
    {
        var threshold = Threshold.Create(TestData.RoomId, MetricType.Co2, new ThresholdLimits(null, null, 1000, 1500), TestData.Now);

        threshold.Update(new ThresholdLimits(null, null, 1200, 1750));

        Assert.Null(threshold.Evaluate(1100));
        Assert.Equal(AlertLevel.Critical, threshold.Evaluate(1800));
    }
}
