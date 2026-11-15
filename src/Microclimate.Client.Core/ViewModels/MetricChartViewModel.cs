using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Microclimate.Client.Core.Presentation;
using Microclimate.Contracts.Analytics;
using Microclimate.Contracts.Common;
using Microclimate.Contracts.Rooms;
using Microclimate.Contracts.Thresholds;

namespace Microclimate.Client.Core.ViewModels;

public enum MetricChartState
{
    NoData,
    Normal,
    Warning,
    Critical,
}

public partial class MetricChartViewModel(MetricOption metric) : ObservableObject
{
    public MetricType Metric => metric.Value;
    public string Name => metric.Name;
    public string Unit => metric.Unit;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSeries))]
    [NotifyPropertyChangedFor(nameof(HasNoSeries))]
    private IReadOnlyList<IReadOnlyList<ChartPoint>> series = [];

    [ObservableProperty]
    private ThresholdResponse? thresholds;

    [ObservableProperty]
    private string summaryText = "Немає даних";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    private MetricChartState state;

    public bool HasSeries => Series.Count > 0;
    public bool HasNoSeries => !HasSeries;
    public string StatusText =>
        State switch
        {
            MetricChartState.Normal => "Зараз · у межах норми",
            MetricChartState.Warning => "Зараз · некомфортно",
            MetricChartState.Critical => "Зараз · критично",
            _ => "Немає актуальної оцінки",
        };

    public void Clear()
    {
        Series = [];
        Thresholds = null;
        SummaryText = "Немає даних";
        State = MetricChartState.NoData;
    }

    public void Update(
        MetricSummaryResponse summary,
        IReadOnlyList<SeriesPointResponse> points,
        LatestMetricResponse? latest,
        ThresholdResponse? thresholds,
        DateTimeOffset now
    )
    {
        Thresholds = thresholds;
        Series = AnalyticsPresentation.SplitSeries(points);
        static string Format(double? value) => value?.ToString("0.#", CultureInfo.InvariantCulture) ?? "—";
        SummaryText =
            summary.Count > 0
                ? $"Мін {Format(summary.Min)} · Сер {Format(summary.Avg)} · Макс {Format(summary.Max)} {Unit}"
                : "Немає даних";
        State = Evaluate(latest, thresholds, now);
    }

    // Підпис поточного стану використовує лише актуальний вимір; історія оцінюється окремо.
    private static MetricChartState Evaluate(
        LatestMetricResponse? latest,
        ThresholdResponse? thresholds,
        DateTimeOffset now
    )
    {
        if (
            latest is null
            || thresholds is null
            || !double.IsFinite(latest.Value)
            || latest.MeasuredAt < now.AddMinutes(-15)
            || latest.MeasuredAt > now.AddMinutes(5)
        )
            return MetricChartState.NoData;
        return ThresholdPresentation.Level(latest.Value, thresholds) switch
        {
            AlertLevel.Critical => MetricChartState.Critical,
            AlertLevel.Warning => MetricChartState.Warning,
            _ => MetricChartState.Normal,
        };
    }
}
