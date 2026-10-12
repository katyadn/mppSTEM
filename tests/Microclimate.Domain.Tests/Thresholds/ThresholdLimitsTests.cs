using Microclimate.Domain.Common;
using Microclimate.Domain.Enums;
using Microclimate.Domain.Thresholds;

namespace Microclimate.Domain.Tests.Thresholds;

public class ThresholdLimitsTests
{
    private static readonly ThresholdLimits Office = new(16, 20, 25, 28);

    [Theory]
    [InlineData(22, null)]
    [InlineData(20, null)]
    [InlineData(25, null)]
    [InlineData(25.01, AlertLevel.Warning)]
    [InlineData(19.99, AlertLevel.Warning)]
    [InlineData(28, AlertLevel.Warning)]
    [InlineData(16, AlertLevel.Warning)]
    [InlineData(28.01, AlertLevel.Critical)]
    [InlineData(15.99, AlertLevel.Critical)]
    public void Evaluate_ValueAgainstOfficeTemperature_ReturnsExpectedLevel(double value, AlertLevel? expected)
    {
        // Act
        var level = Office.Evaluate(value);

        // Assert
        Assert.Equal(expected, level);
    }

    [Theory]
    [InlineData(1200, null)]
    [InlineData(1200.5, AlertLevel.Warning)]
    [InlineData(1750, AlertLevel.Warning)]
    [InlineData(1751, AlertLevel.Critical)]
    [InlineData(0, null)]
    public void Evaluate_UpperBoundsOnly_IgnoresMissingLowerBounds(double value, AlertLevel? expected)
    {
        var co2 = new ThresholdLimits(null, null, 1200, 1750);

        Assert.Equal(expected, co2.Evaluate(value));
    }

    [Theory]
    [InlineData(22, null)]
    [InlineData(26, 25.0)]
    [InlineData(29, 28.0)]
    [InlineData(18, 20.0)]
    [InlineData(10, 16.0)]
    public void ExceededBound_Value_ReturnsNearestViolatedBound(double value, double? expected)
    {
        Assert.Equal(expected, Office.ExceededBound(value));
    }

    [Fact]
    public void Constructor_AllBoundsMissing_Throws()
    {
        Assert.Throws<DomainException>(() => new ThresholdLimits(null, null, null, null));
    }

    [Theory]
    [InlineData(21.0, 20.0, 25.0, 28.0)]
    [InlineData(16.0, 20.0, 25.0, 24.0)]
    [InlineData(16.0, 25.0, 25.0, 28.0)]
    [InlineData(16.0, 26.0, 25.0, 28.0)]
    [InlineData(30.0, null, null, 28.0)]
    [InlineData(null, 30.0, null, 28.0)]
    public void Constructor_BoundsOutOfOrder_Throws(double? criticalMin, double? warningMin, double? warningMax, double? criticalMax)
    {
        Assert.Throws<DomainException>(() => new ThresholdLimits(criticalMin, warningMin, warningMax, criticalMax));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Constructor_NotFiniteBound_Throws(double bound)
    {
        Assert.Throws<DomainException>(() => new ThresholdLimits(null, null, bound, null));
    }

    [Fact]
    public void Constructor_EqualWarningAndCriticalBounds_IsAllowed()
    {
        var limits = new ThresholdLimits(20, 20, 25, 25);

        Assert.Equal(AlertLevel.Critical, limits.Evaluate(25.1));
        Assert.Null(limits.Evaluate(25));
    }
}
