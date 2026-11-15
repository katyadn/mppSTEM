using Microclimate.Client.Core.Presentation;
using Microclimate.Contracts.Common;
using Microclimate.Contracts.Thresholds;

namespace Microclimate.Client.Tests;

public sealed class ThresholdPresentationTests
{
    private static readonly ThresholdResponse Thresholds = new(
        Guid.Empty,
        ClientFixture.RoomId,
        MetricType.Temperature,
        20,
        25,
        16,
        28
    );

    [Theory]
    [InlineData(14, 30)]
    [InlineData(30, 14)]
    public void LineChangesColorAtEachCrossedBoundary(double from, double to)
    {
        var start = new TestClock().Now;
        var parts = ThresholdPresentation.SplitLine(new(start, from), new(start.AddHours(1), to), Thresholds);
        Assert.Equal(
            new AlertLevel?[]
            {
                AlertLevel.Critical,
                AlertLevel.Warning,
                null,
                AlertLevel.Warning,
                AlertLevel.Critical,
            },
            parts.Select(part => part.Level)
        );
        Assert.Equal(
            from < to ? new double[] { 16, 20, 25, 28 } : new double[] { 28, 25, 20, 16 },
            parts.SkipLast(1).Select(part => part.End.Value)
        );
        Assert.Equal(start, parts[0].Start.Time);
        Assert.Equal(start.AddHours(1), parts[^1].End.Time);
        Assert.All(parts.Zip(parts.Skip(1)), pair => Assert.Equal(pair.First.End, pair.Second.Start));
        Assert.All(parts, part => Assert.True(part.Start.Time < part.End.Time));
    }

    [Theory]
    [InlineData(25, 25, null)]
    [InlineData(26, 27, AlertLevel.Warning)]
    [InlineData(29, 30, AlertLevel.Critical)]
    [InlineData(20, 25, null)]
    public void ALineWithinOneBandKeepsItsColor(double from, double to, AlertLevel? level)
    {
        var start = new TestClock().Now;
        var part = Assert.Single(
            ThresholdPresentation.SplitLine(new(start, from), new(start.AddHours(1), to), Thresholds)
        );
        Assert.Equal(level, part.Level);
    }

    [Fact]
    public void MissingAndCoincidentBoundsDoNotCreateEmptySections()
    {
        var start = new ChartPoint(new TestClock().Now, 15);
        var end = new ChartPoint(start.Time.AddHours(1), 30);
        Assert.Null(Assert.Single(ThresholdPresentation.SplitLine(start, end, null)).Level);
        var parts = ThresholdPresentation.SplitLine(
            start,
            end,
            Thresholds with
            {
                WarningMin = null,
                CriticalMin = null,
                CriticalMax = 25,
            }
        );
        Assert.Equal(new AlertLevel?[] { null, AlertLevel.Critical }, parts.Select(part => part.Level));
        Assert.All(parts, part => Assert.NotEqual(part.Start, part.End));
    }
}
