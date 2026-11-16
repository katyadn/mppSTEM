using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microclimate.Client.Core.Presentation;
using Microclimate.Client.Core.Services;
using Microclimate.Contracts.Common;

namespace Microclimate.Client.Core.ViewModels;

public sealed record MetricOption(MetricType Value, string Name, string Unit);

public sealed record PeriodOption(int Days, string Name);

// Завантажує статистику, сегменти графіка й комфорт вибраної кімнати за поточний період.
public partial class RoomDetailsViewModel : BaseViewModel
{
    private readonly IClientApi api;

    private readonly INavigationService navigation;

    private readonly TimeProvider clock;

    [ObservableProperty]
    private string comfortText = "Немає даних";

    [ObservableProperty]
    private string rangeText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsThresholdEditorClosed))]
    private bool isThresholdEditorOpen;

    public bool IsThresholdEditorClosed => !IsThresholdEditorOpen;
    public string ThresholdEditorTitle => $"Пороги · {SelectedMetric.Name}";
    public IReadOnlyList<MetricChartViewModel> Charts { get; }

    // Метрика відкритого редактора порогів; усі графіки завантажуються разом.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ThresholdEditorTitle))]
    private MetricOption selectedMetric;

    // Спільний період усіх шести графіків.
    [ObservableProperty]
    private PeriodOption selectedPeriod;

    public IReadOnlyList<MetricOption> Metrics { get; } =
    [
        new(MetricType.Temperature, "Температура", "°C"),
        new(MetricType.Humidity, "Вологість", "%"),
        new(MetricType.Co2, "CO₂", "ppm"),
        new(MetricType.Pm25, "PM2.5", "µg/m³"),
        new(MetricType.Noise, "Шум", "dB(A)"),
        new(MetricType.Light, "Освітленість", "lux"),
    ];

    public IReadOnlyList<PeriodOption> Periods { get; } = [new(1, "24 години"), new(7, "7 днів"), new(30, "30 днів")];

    public Guid? RoomId { get; private set; }

    private bool initialized;
    private Task activeRefresh = Task.CompletedTask;
    private int selectionVersion;
    public ThresholdsViewModel Thresholds { get; }
    public Task SelectionRefresh { get; private set; } = Task.CompletedTask;

    partial void OnSelectedMetricChanged(MetricOption value)
    {
        Thresholds.Invalidate();
        if (initialized)
            SelectionRefresh = LoadSelectedThresholdsAsync();
    }

    partial void OnSelectedPeriodChanged(PeriodOption value)
    {
        if (initialized)
            SelectionRefresh = RefreshSelectionAsync();
    }

    private async Task RefreshSelectionAsync()
    {
        var version = ++selectionVersion;
        CancelPending();
        await activeRefresh;
        if (version == selectionVersion)
            await StartRefreshAsync(live: false);
    }

    public RoomDetailsViewModel(
        IClientApi api,
        INavigationService navigation,
        TimeProvider clock,
        ClientSession session
    )
        : base(navigation)
    {
        this.api = api;
        this.navigation = navigation;
        this.clock = clock;
        Thresholds = new ThresholdsViewModel(api, session, navigation);
        Charts = Metrics.Select(metric => new MetricChartViewModel(metric)).ToArray();
        selectedMetric = Metrics[0];
        selectedPeriod = Periods[0];
        Title = "Деталі кімнати";
    }

    // Запам’ятовує кімнату й очищає попередню статистику.
    public void SelectRoom(Guid? roomId, string? name)
    {
        initialized = false;
        Thresholds.Invalidate();
        RoomId = roomId;
        Title = string.IsNullOrWhiteSpace(name) ? "Деталі кімнати" : name;
        foreach (var chart in Charts)
            chart.Clear();
        IsThresholdEditorOpen = false;
        ComfortText = "Немає даних";
    }

    public override async Task InitializeAsync()
    {
        await RefreshCommand.ExecuteAsync(null);
        initialized = true;
    }

    // Очищає старі дані та завантажує всі показники за один UTC-період.
    [RelayCommand]
    private Task RefreshAsync() => StartRefreshAsync(live: false);

    public Task RefreshLiveAsync() => StartRefreshAsync(live: true);

    private Task StartRefreshAsync(bool live)
    {
        activeRefresh = RunAsync(token => LoadAsync(live, token));
        return activeRefresh;
    }

    private async Task LoadAsync(bool live, CancellationToken token)
    {
        if (RoomId is not { } roomId)
        {
            ErrorMessage = "Спершу виберіть кімнату в огляді.";
            return;
        }
        if (!live)
        {
            foreach (var chart in Charts)
                chart.Clear();
            ComfortText = "Немає даних";
        }
        var to = clock.GetUtcNow();
        var from = to.AddDays(-SelectedPeriod.Days);
        var roomTask = api.GetRoomAsync(roomId, token);
        var comfortTask = api.GetComfortAsync(roomId, token);
        var latestTask = api.GetLatestAsync(roomId, token);
        var thresholdsTask = api.GetThresholdsAsync(roomId, token);
        var chartTasks = Charts
            .Select(async chart =>
            {
                var summaryTask = api.GetSummaryAsync(roomId, chart.Metric, from, to, token);
                var pointsTask = api.GetSeriesAsync(roomId, chart.Metric, from, to, token);
                await Task.WhenAll(summaryTask, pointsTask);
                return (Chart: chart, Summary: await summaryTask, Points: await pointsTask);
            })
            .ToArray();
        await Task.WhenAll(chartTasks.Cast<Task>().Concat([roomTask, comfortTask, latestTask, thresholdsTask]));
        token.ThrowIfCancellationRequested();
        Title = (await roomTask).Name;
        RangeText = $"{from.ToLocalTime():dd.MM HH:mm} — {to.ToLocalTime():dd.MM HH:mm}";
        ComfortText = AnalyticsPresentation.ComfortText(await comfortTask);
        var latest = await latestTask;
        var thresholds = await thresholdsTask;
        foreach (var task in chartTasks)
        {
            var result = await task;
            result.Chart.Update(
                result.Summary,
                result.Points,
                latest.Where(item => item.Metric == result.Chart.Metric).MaxBy(item => item.MeasuredAt),
                thresholds.FirstOrDefault(item => item.Metric == result.Chart.Metric),
                to
            );
        }
    }

    private async Task LoadSelectedThresholdsAsync()
    {
        if (RoomId is not { } roomId)
            return;
        await Thresholds.LoadAsync(roomId, SelectedMetric.Value);
    }

    [RelayCommand]
    private async Task OpenThresholdsAsync(MetricChartViewModel chart)
    {
        if (RoomId is null)
            return;
        IsThresholdEditorOpen = true;
        if (SelectedMetric.Value != chart.Metric)
        {
            SelectedMetric = Metrics.First(item => item.Value == chart.Metric);
            if (initialized)
                await SelectionRefresh;
            else
                await LoadSelectedThresholdsAsync();
        }
        else
            await LoadSelectedThresholdsAsync();
    }

    [RelayCommand]
    private void CloseThresholds()
    {
        if (Thresholds.IsBusy)
            return;
        IsThresholdEditorOpen = false;
        Thresholds.Invalidate();
    }

    [RelayCommand]
    private async Task SaveThresholdsAsync()
    {
        if (!Thresholds.CanSave)
            return;
        await Thresholds.SaveCommand.ExecuteAsync(null);
        if (Thresholds.HasError || string.IsNullOrEmpty(Thresholds.Message))
            return;
        IsThresholdEditorOpen = false;
        await activeRefresh;
        await RefreshLiveAsync();
    }

    [RelayCommand]
    private Task GoBackAsync() => navigation.GoBackAsync();
}
