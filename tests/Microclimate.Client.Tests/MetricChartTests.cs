using Microclimate.Client.Core.ViewModels;
using Microclimate.Contracts.Analytics;
using Microclimate.Contracts.Common;
using Microclimate.Contracts.Rooms;
using Microclimate.Contracts.Thresholds;

namespace Microclimate.Client.Tests;

public sealed class MetricChartTests
{
    [Theory]
    [InlineData(16, MetricChartState.Warning)]
    [InlineData(15.9, MetricChartState.Critical)]
    [InlineData(20, MetricChartState.Normal)]
    [InlineData(25, MetricChartState.Normal)]
    [InlineData(25.1, MetricChartState.Warning)]
    [InlineData(28, MetricChartState.Warning)]
    [InlineData(28.1, MetricChartState.Critical)]
    public void LatestMeasurementUsesInclusiveServerThresholds(double value, MetricChartState expected)
    {
        var now = new TestClock().Now;
        var chart = new MetricChartViewModel(new(MetricType.Temperature, "Температура", "°C"));
        chart.Update(
            new(MetricType.Temperature, 1, 22, 22, 22, null),
            [new(now, 1, 22, 22, 22)],
            new(MetricType.Temperature, value, now),
            new(Guid.Empty, ClientFixture.RoomId, MetricType.Temperature, 20, 25, 16, 28),
            now
        );
        Assert.Equal(expected, chart.State);
    }

    [Theory]
    [InlineData(-16, 22)]
    [InlineData(6, 22)]
    [InlineData(0, double.NaN)]
    public void HistoricalPointsDoNotCreateAFreshStatus(int ageMinutes, double latestValue)
    {
        var now = new TestClock().Now;
        var chart = new MetricChartViewModel(new(MetricType.Temperature, "Температура", "°C"));
        chart.Update(
            new(MetricType.Temperature, 1, 22, 22, 22, null),
            [new(now.AddHours(-1), 1, 22, 22, 22)],
            new(MetricType.Temperature, latestValue, now.AddMinutes(ageMinutes)),
            new(Guid.Empty, ClientFixture.RoomId, MetricType.Temperature, 20, 25, 16, 28),
            now
        );
        Assert.True(chart.HasSeries);
        Assert.Equal(MetricChartState.NoData, chart.State);
    }

    [Fact]
    public void ZeroIsARealMeasurementAndMissingThresholdsHaveNoStatus()
    {
        var now = new TestClock().Now;
        var chart = new MetricChartViewModel(new(MetricType.Pm25, "PM2.5", "µg/m³"));
        var summary = new MetricSummaryResponse(MetricType.Pm25, 1, 0, 0, 0, null);
        SeriesPointResponse[] points = [new(now, 1, 0, 0, 0)];
        var latest = new LatestMetricResponse(MetricType.Pm25, 0, now);
        var thresholds = new ThresholdResponse(Guid.Empty, ClientFixture.RoomId, MetricType.Pm25, null, 25, null, 50);
        chart.Update(summary, points, latest, thresholds, now);
        Assert.Equal(MetricChartState.Normal, chart.State);
        Assert.Equal(0, Assert.Single(Assert.Single(chart.Series)).Value);
        chart.Update(summary, points, latest, null, now);
        Assert.Equal(MetricChartState.NoData, chart.State);
        chart.Update(summary, points, null, thresholds, now);
        Assert.Equal(MetricChartState.NoData, chart.State);
    }

    [Fact]
    public async Task CancelDiscardsDraftAndInvalidSaveKeepsEditorOpen()
    {
        using var f = new ClientFixture();
        await f.SignInAsync();
        var vm = new RoomDetailsViewModel(f.Api, f.Navigation, f.Clock, f.Session);
        vm.SelectRoom(ClientFixture.RoomId, "101");
        await vm.InitializeAsync();
        await vm.OpenThresholdsCommand.ExecuteAsync(vm.Charts[0]);
        Assert.True(vm.IsThresholdEditorOpen);
        vm.Thresholds.WarningMax = "100";
        await vm.SaveThresholdsCommand.ExecuteAsync(null);
        Assert.True(vm.IsThresholdEditorOpen);
        Assert.True(vm.Thresholds.HasError);
        vm.CloseThresholdsCommand.Execute(null);
        Assert.False(vm.IsThresholdEditorOpen);
        await vm.OpenThresholdsCommand.ExecuteAsync(vm.Charts[0]);
        Assert.Equal("25", vm.Thresholds.WarningMax);
        Assert.False(vm.Thresholds.HasError);
        Assert.DoesNotContain(f.Requests, request => request.Method == "PUT");
    }
}
