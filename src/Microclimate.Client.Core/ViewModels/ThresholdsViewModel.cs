using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microclimate.Client.Core.Presentation;
using Microclimate.Client.Core.Services;
using Microclimate.Contracts.Common;
using Microclimate.Contracts.Thresholds;

namespace Microclimate.Client.Core.ViewModels;

// Межі редагуються окремо від живого графіка: фонове оновлення не стирає введені значення.
public partial class ThresholdsViewModel(IClientApi api, ClientSession session, INavigationService navigation) : BaseViewModel(navigation)
{
    private Guid roomId;
    private MetricType metric;
    private int contextVersion;
    [ObservableProperty] private string warningMin = string.Empty;
    [ObservableProperty] private string warningMax = string.Empty;
    [ObservableProperty] private string criticalMin = string.Empty;
    [ObservableProperty] private string criticalMax = string.Empty;
    [ObservableProperty] private string unit = string.Empty;
    [ObservableProperty] private string message = string.Empty;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanSave))] private bool isLoaded;
    public bool CanEdit => session.Current?.User.Role is UserRole.Admin or UserRole.Operator;
    public bool CanSave => IsLoaded && CanEdit && IsNotBusy;

    public void Invalidate()
    {
        contextVersion++; CancelPending(); IsLoaded = false;
        WarningMin = WarningMax = CriticalMin = CriticalMax = Message = string.Empty;
    }

    public Task LoadAsync(Guid id, MetricType selectedMetric) => RunAsync(async token =>
    {
        var version = ++contextVersion;
        IsLoaded = false; Message = string.Empty;
        roomId = id; metric = selectedMetric; Unit = MetricPresentation.MetricUnit(metric);
        WarningMin = WarningMax = CriticalMin = CriticalMax = string.Empty;
        OnPropertyChanged(nameof(CanEdit));
        var thresholds = await api.GetThresholdsAsync(id, token);
        token.ThrowIfCancellationRequested();
        if (version != contextVersion) return;
        var value = thresholds.FirstOrDefault(item => item.Metric == selectedMetric);
        if (value is null) { ErrorMessage = "Сервер не повернув порогів для цієї метрики."; return; }
        Apply(value); IsLoaded = true;
    });

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async token =>
    {
        Message = string.Empty;
        if (!CanEdit || !IsLoaded) { ErrorMessage = "Зміна порогів доступна Admin/Operator після завантаження."; return; }
        var version = contextVersion;
        var request = Parse(WarningMin, WarningMax, CriticalMin, CriticalMax);
        var saved = await api.UpdateThresholdAsync(roomId, metric, request, token);
        token.ThrowIfCancellationRequested();
        if (version != contextVersion) return;
        Apply(saved); Message = "Пороги збережено.";
    });

    // Порожнє поле означає відсутність межі. Крапка й кома підтримуються без thousands separator.
    public static UpdateThresholdRequest Parse(string warningMin, string warningMax, string criticalMin, string criticalMax)
    {
        static double? Number(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (!double.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !double.IsFinite(value))
                throw new ArgumentException("Введіть скінченне число або залиште поле порожнім.");
            return value;
        }
        var request = new UpdateThresholdRequest(Number(warningMin), Number(warningMax), Number(criticalMin), Number(criticalMax));
        if (request is { WarningMin: null, WarningMax: null, CriticalMin: null, CriticalMax: null })
            throw new ArgumentException("Задайте хоча б одну межу.");
        var ordered = new[] { request.CriticalMin, request.WarningMin, request.WarningMax, request.CriticalMax }.Where(value => value.HasValue).Select(value => value!.Value).ToArray();
        if (ordered.Zip(ordered.Skip(1)).Any(pair => pair.First > pair.Second) ||
            new[] { request.CriticalMin, request.WarningMin }.Any(low => new[] { request.WarningMax, request.CriticalMax }.Any(high => low.HasValue && high.HasValue && low >= high)))
            throw new ArgumentException("Межі мають відповідати порядку: критична нижня ≤ нижня попередження < верхня попередження ≤ критична верхня.");
        return request;
    }

    private void Apply(ThresholdResponse value)
    {
        static string Format(double? number) => number?.ToString("G", CultureInfo.InvariantCulture) ?? string.Empty;
        WarningMin = Format(value.WarningMin); WarningMax = Format(value.WarningMax);
        CriticalMin = Format(value.CriticalMin); CriticalMax = Format(value.CriticalMax);
    }

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(IsBusy)) OnPropertyChanged(nameof(CanSave));
    }
}
