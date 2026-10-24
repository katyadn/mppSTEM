using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microclimate.Client.Core.Presentation;
using Microclimate.Client.Core.Services;
using Microclimate.Contracts.Common;

namespace Microclimate.Client.Core.ViewModels;

public sealed record MetricOption(MetricType Value, string Name, string Unit);
public sealed record PeriodOption(int Days, string Name);

public partial class RoomDetailsViewModel : BaseViewModel
{
    private readonly IClientApi api;
    private readonly INavigationService navigation;
    private readonly TimeProvider clock;
    [ObservableProperty] private string comfortText = "Немає даних";
    [ObservableProperty] private string summaryText = "Немає даних";
    [ObservableProperty] private string rangeText = string.Empty;
    [ObservableProperty] private string chartUnit = string.Empty;
    [ObservableProperty] private IReadOnlyList<IReadOnlyList<ChartPoint>> series = [];
    [ObservableProperty] private bool hasSeries;
    [ObservableProperty] private bool hasNoSeries = true;
    [ObservableProperty] private MetricOption selectedMetric;
    [ObservableProperty] private PeriodOption selectedPeriod;

    public IReadOnlyList<MetricOption> Metrics { get; } = [
        new(MetricType.Temperature, "Температура", "°C"), new(MetricType.Humidity, "Вологість", "%"),
        new(MetricType.Co2, "CO₂", "ppm"), new(MetricType.Pm25, "PM2.5", "µg/m³"),
        new(MetricType.Noise, "Шум", "dB(A)"), new(MetricType.Light, "Освітленість", "lux")];
    public IReadOnlyList<PeriodOption> Periods { get; } = [new(1, "24 години"), new(7, "7 днів"), new(30, "30 днів")];
    public Guid? RoomId { get; private set; }

    public RoomDetailsViewModel(IClientApi api, INavigationService navigation, TimeProvider clock) : base(navigation)
    {
        this.api = api; this.navigation = navigation; this.clock = clock;
        selectedMetric = Metrics[0]; selectedPeriod = Periods[0]; Title = "Деталі кімнати";
    }
    public void SelectRoom(Guid? roomId, string? name)
    {
        RoomId = roomId; Title = string.IsNullOrWhiteSpace(name) ? "Деталі кімнати" : name;
        Series = []; HasSeries = false; HasNoSeries = true; ComfortText = "Немає даних"; SummaryText = "Немає даних";
    }
    public override Task InitializeAsync() => RefreshCommand.ExecuteAsync(null);
    [RelayCommand]
    private Task RefreshAsync() => RunAsync(async token =>
    {
        if (RoomId is not { } roomId) { ErrorMessage = "Спершу виберіть кімнату в огляді."; return; }
        // Очищаємо старі дані, щоб помилка нового запиту не показувала попередній період.
        Series = []; HasSeries = false; HasNoSeries = true; SummaryText = "Немає даних"; ComfortText = "Немає даних";
        var to = clock.GetUtcNow(); var from = to.AddDays(-SelectedPeriod.Days);
        var metric = SelectedMetric;
        ChartUnit = metric.Unit;
        RangeText = $"{from.ToLocalTime():dd.MM HH:mm} — {to.ToLocalTime():dd.MM HH:mm} · {metric.Unit}";
        var room = await api.GetRoomAsync(roomId, token);
        var summary = await api.GetSummaryAsync(roomId, metric.Value, from, to, token);
        var points = await api.GetSeriesAsync(roomId, metric.Value, from, to, token);
        var comfort = await api.GetComfortAsync(roomId, token);
        token.ThrowIfCancellationRequested();
        Title = room.Name;
        Series = AnalyticsPresentation.SplitSeries(points); HasSeries = Series.Count > 0; HasNoSeries = !HasSeries;
        ComfortText = AnalyticsPresentation.ComfortText(comfort);
        if (summary.Count > 0)
        {
            static string Format(double? value) => value?.ToString("0.#", CultureInfo.InvariantCulture) ?? "—";
            SummaryText = $"Мін: {Format(summary.Min)} · Середнє: {Format(summary.Avg)} · Макс: {Format(summary.Max)} {metric.Unit}";
        }
    });
    [RelayCommand] private Task GoBackAsync() => navigation.GoBackAsync();
}
