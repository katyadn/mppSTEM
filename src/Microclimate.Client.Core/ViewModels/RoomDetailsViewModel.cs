using System.Globalization;
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
    private string summaryText = "Немає даних";

    // Період завантажених даних у місцевому часі та одиниця метрики.
    [ObservableProperty]
    private string rangeText = string.Empty;

    // Одиниця завантаженої серії; не змінюється від самого вибору іншої метрики.
    [ObservableProperty]
    private string chartUnit = string.Empty;

    // Неперервні сегменти графіка; пропущені інтервали розділяють сегменти.
    [ObservableProperty]
    private IReadOnlyList<IReadOnlyList<ChartPoint>> series = [];

    [ObservableProperty]
    private bool hasSeries;

    [ObservableProperty]
    private bool hasNoSeries = true;

    // Вибір метрики, що застосовується при наступному оновленні.
    [ObservableProperty]
    private MetricOption selectedMetric;

    // Вибір періоду, що застосовується при наступному оновленні.
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
    private MetricType? thresholdMetric;
    private Task activeRefresh = Task.CompletedTask;
    private int selectionVersion;
    public ThresholdsViewModel Thresholds { get; }
    public Task SelectionRefresh { get; private set; } = Task.CompletedTask;

    partial void OnSelectedMetricChanged(MetricOption value)
    {
        Thresholds.Invalidate();
        thresholdMetric = null;
        if (initialized)
            SelectionRefresh = RefreshSelectionAsync();
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
        selectedMetric = Metrics[0];
        selectedPeriod = Periods[0];
        Title = "Деталі кімнати";
    }

    // Запам’ятовує кімнату й очищає попередню статистику та графік.
    public void SelectRoom(Guid? roomId, string? name)
    {
        initialized = false;
        thresholdMetric = null;
        Thresholds.Invalidate();
        RoomId = roomId;
        Title = string.IsNullOrWhiteSpace(name) ? "Деталі кімнати" : name;
        Series = [];
        HasSeries = false;
        HasNoSeries = true;
        ComfortText = "Немає даних";
        SummaryText = "Немає даних";
    }

    public override async Task InitializeAsync()
    {
        await RefreshCommand.ExecuteAsync(null);
        initialized = true;
    }

    // Очищає старі дані, фіксує метрику й UTC-період та завантажує кімнату, статистику, ряд і комфорт.
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
        // Новий період не показує старих даних; live update зберігає висоту графіка й вибрану точку.
        if (!live)
        {
            Series = [];
            HasSeries = false;
            HasNoSeries = true;
            SummaryText = "Немає даних";
            ComfortText = "Немає даних";
        }
        var to = clock.GetUtcNow();
        var from = to.AddDays(-SelectedPeriod.Days);
        var metric = SelectedMetric;
        if (!Thresholds.IsLoaded || thresholdMetric != metric.Value)
        {
            // Пороги завантажуються незалежно від успіху запитів аналітики.
            await Thresholds.LoadAsync(roomId, metric.Value);
            token.ThrowIfCancellationRequested();
            thresholdMetric = metric.Value;
        }
        var room = await api.GetRoomAsync(roomId, token);
        var summary = await api.GetSummaryAsync(roomId, metric.Value, from, to, token);
        var points = await api.GetSeriesAsync(roomId, metric.Value, from, to, token);
        var comfort = await api.GetComfortAsync(roomId, token);
        token.ThrowIfCancellationRequested();
        Title = room.Name;
        ChartUnit = metric.Unit;
        RangeText = $"{from.ToLocalTime():dd.MM HH:mm} — {to.ToLocalTime():dd.MM HH:mm} · {metric.Unit}";
        Series = AnalyticsPresentation.SplitSeries(points);
        HasSeries = Series.Count > 0;
        HasNoSeries = !HasSeries;
        ComfortText = AnalyticsPresentation.ComfortText(comfort);
        if (summary.Count > 0)
        {
            // Відсутній агрегат показуємо тире, а не вигаданим нульовим виміром.
            static string Format(double? value) => value?.ToString("0.#", CultureInfo.InvariantCulture) ?? "—";
            SummaryText =
                $"Мін: {Format(summary.Min)} · Середнє: {Format(summary.Avg)} · Макс: {Format(summary.Max)} {metric.Unit}";
        }
        else
            SummaryText = "Немає даних";
    }

    [RelayCommand]
    private Task GoBackAsync() => navigation.GoBackAsync();
}
