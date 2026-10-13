using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microclimate.Client.Core.Presentation;
using Microclimate.Client.Maui.Services;
using Microclimate.Contracts.Analytics;

namespace Microclimate.Client.Maui.ViewModels;

public partial class RoomDetailsViewModel : BaseViewModel
{
    private readonly INavigationService navigationService;

    [ObservableProperty]
    private string comfortText = "Немає даних";

    [ObservableProperty]
    private IReadOnlyList<IReadOnlyList<ChartPoint>> series = [];

    [ObservableProperty]
    private bool hasSeries;

    [ObservableProperty]
    private bool hasNoSeries = true;

    public RoomDetailsViewModel(INavigationService navigationService)
    {
        this.navigationService = navigationService;
        Title = "Деталі кімнати";
    }

    public Guid? RoomId { get; private set; }

    public void SelectRoom(Guid? roomId, string? name)
    {
        RoomId = roomId;
        Title = string.IsNullOrWhiteSpace(name) ? "Деталі кімнати" : name;
        ApplyAnalytics([], null);
    }

    public void ApplyAnalytics(IEnumerable<SeriesPointResponse> points, ComfortIndexResponse? comfort)
    {
        Series = AnalyticsPresentation.SplitSeries(points);
        HasSeries = Series.Count > 0;
        HasNoSeries = !HasSeries;
        ComfortText = AnalyticsPresentation.ComfortText(comfort);
    }

    [RelayCommand]
    private Task GoBackAsync() => navigationService.GoBackAsync();
}
