using Microclimate.Client.Core.Presentation;
using Microclimate.Contracts.Analytics;

namespace Microclimate.Client.Tests;

public sealed class AnalyticsPresentationTests
{
    [Fact]
    public void EmptyBucketSeparatesChartSegments()
    {
        var start = DateTimeOffset.Parse("2026-10-13T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        SeriesPointResponse[] points = [new(start, 1, 20, 20, 20), new(start.AddHours(1), 0, 99, null, null), new(start.AddHours(2), 1, 24, 24, 24)];
        var segments = AnalyticsPresentation.SplitSeries(points);
        Assert.Equal(2, segments.Count);
        Assert.Equal(20, Assert.Single(segments[0]).Value);
        Assert.Equal(24, Assert.Single(segments[1]).Value);
    }

    [Fact]
    public void MissingAndNonFiniteAveragesBreakTheLine()
    {
        var start = DateTimeOffset.UtcNow;
        SeriesPointResponse[] points = [new(start, 1, null, null, null), new(start.AddHours(1), 1, double.NaN, null, null)];
        Assert.Empty(AnalyticsPresentation.SplitSeries(points));
    }

    [Fact]
    public void SeriesUsesChronologicalOrderAndKeepsMeasuredZero()
    {
        var start = DateTimeOffset.UtcNow;
        SeriesPointResponse[] points = [new(start.AddHours(1), 1, 10, 10, 10), new(start, 1, 0, 0, 0)];
        var segment = Assert.Single(AnalyticsPresentation.SplitSeries(points));
        Assert.Equal(new double[] { 0, 10 }, segment.Select(point => point.Value));
    }

    [Fact]
    public void NoDataDoesNotBecomeZeroComfort()
    {
        var response = new ComfortIndexResponse(0, ComfortCategory.NoData, []);
        Assert.Equal("Немає даних", AnalyticsPresentation.ComfortText(response));
        Assert.Equal("Немає даних", AnalyticsPresentation.ComfortText(null));
    }

    [Fact]
    public void ActualZeroComfortIsDisplayedAsAScore()
    {
        var response = new ComfortIndexResponse(0, ComfortCategory.Uncomfortable, []);
        Assert.Equal("0/100 · Некомфортно", AnalyticsPresentation.ComfortText(response));
    }

    [Fact]
    public void ClientPreservesServerCategoryInsteadOfRecalculatingThresholds()
    {
        var response = new ComfortIndexResponse(82, ComfortCategory.Acceptable, []);
        Assert.Equal("82/100 · Прийнятно", AnalyticsPresentation.ComfortText(response));
    }
}
