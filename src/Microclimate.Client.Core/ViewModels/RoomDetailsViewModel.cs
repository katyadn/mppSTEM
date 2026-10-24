using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microclimate.Client.Core.Presentation;
using Microclimate.Client.Core.Services;
using Microclimate.Contracts.Common;

namespace Microclimate.Client.Core.ViewModels;

// Пункт вибору метрики з назвою та одиницею для аналітики.
public sealed record MetricOption(MetricType Value, string Name, string Unit);
// Пункт вибору тривалості аналітичного запиту в днях.
public sealed record PeriodOption(int Days, string Name);

// Завантажує статистику, сегменти графіка й комфорт вибраної кімнати за поточний період.
public partial class RoomDetailsViewModel : BaseViewModel
{
    // Клієнтський API для завантаження даних екрана.
    private readonly IClientApi api;
    // Сервіс переходів між екранами.
    private readonly INavigationService navigation;
    // Джерело поточного UTC-часу.
    private readonly TimeProvider clock;
    // Спостережуваний текст індексу та серверної категорії комфорту.
    [ObservableProperty] private string comfortText = "Немає даних";
    // Спостережуваний текст мінімуму, середнього й максимуму або відсутності даних.
    [ObservableProperty] private string summaryText = "Немає даних";
    // Період завантажених даних у місцевому часі та одиниця метрики.
    [ObservableProperty] private string rangeText = string.Empty;
    // Одиниця завантаженої серії; не змінюється від самого вибору іншої метрики.
    [ObservableProperty] private string chartUnit = string.Empty;
    // Неперервні сегменти графіка; пропущені інтервали розділяють сегменти.
    [ObservableProperty] private IReadOnlyList<IReadOnlyList<ChartPoint>> series = [];
    // Спостережувана ознака наявності точок для графіка.
    [ObservableProperty] private bool hasSeries;
    // Спостережувана ознака показу порожнього стану замість графіка.
    [ObservableProperty] private bool hasNoSeries = true;
    // Вибір метрики, що застосовується при наступному оновленні.
    [ObservableProperty] private MetricOption selectedMetric;
    // Вибір періоду, що застосовується при наступному оновленні.
    [ObservableProperty] private PeriodOption selectedPeriod;

    // Шість підтримуваних метрик із назвами та одиницями.
    public IReadOnlyList<MetricOption> Metrics { get; } = [
        new(MetricType.Temperature, "Температура", "°C"), new(MetricType.Humidity, "Вологість", "%"),
        new(MetricType.Co2, "CO₂", "ppm"), new(MetricType.Pm25, "PM2.5", "µg/m³"),
        new(MetricType.Noise, "Шум", "dB(A)"), new(MetricType.Light, "Освітленість", "lux")];
    // Підтримувані періоди: 24 години, 7 днів і 30 днів.
    public IReadOnlyList<PeriodOption> Periods { get; } = [new(1, "24 години"), new(7, "7 днів"), new(30, "30 днів")];
    // Ідентифікатор кімнати для запитів аналітики або переходів.
    public Guid? RoomId { get; private set; }

    // Ініціалізує RoomDetailsViewModel та його залежності для клієнтського сценарію.
    public RoomDetailsViewModel(IClientApi api, INavigationService navigation, TimeProvider clock) : base(navigation)
    {
        this.api = api; this.navigation = navigation; this.clock = clock;
        selectedMetric = Metrics[0]; selectedPeriod = Periods[0]; Title = "Деталі кімнати";
    }
    // Запам’ятовує кімнату й очищає попередню статистику та графік.
    public void SelectRoom(Guid? roomId, string? name)
    {
        RoomId = roomId; Title = string.IsNullOrWhiteSpace(name) ? "Деталі кімнати" : name;
        Series = []; HasSeries = false; HasNoSeries = true; ComfortText = "Немає даних"; SummaryText = "Немає даних";
    }
    // Оновлює стан екрана під час його появи.
    public override Task InitializeAsync() => RefreshCommand.ExecuteAsync(null);
    // Очищає старі дані, фіксує метрику й UTC-період та завантажує кімнату, статистику, ряд і комфорт.
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
            // Відсутній агрегат показуємо тире, а не вигаданим нульовим виміром.
            static string Format(double? value) => value?.ToString("0.#", CultureInfo.InvariantCulture) ?? "—";
            SummaryText = $"Мін: {Format(summary.Min)} · Середнє: {Format(summary.Avg)} · Макс: {Format(summary.Max)} {metric.Unit}";
        }
    });
    // Повертається на попередній екран.
    [RelayCommand] private Task GoBackAsync() => navigation.GoBackAsync();
}
