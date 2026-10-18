using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microclimate.Client.Core.Presentation;
using Microclimate.Client.Core.Services;
using Microclimate.Contracts.Common;

namespace Microclimate.Client.Core.ViewModels;

public sealed record AlertLevelOption(AlertLevel? Value, string Name);
public sealed record AlertStatusOption(AlertStatus? Value, string Name);

public partial class AlertsViewModel : BaseViewModel
{
    private readonly IClientApi api;
    private readonly ClientSession session;
    private readonly INavigationService navigation;
    private int page;
    private AlertLevel? appliedLevel;
    private AlertStatus? appliedStatus;
    [ObservableProperty] private bool hasMore;
    [ObservableProperty] private AlertLevelOption selectedLevel;
    [ObservableProperty] private AlertStatusOption selectedStatus;
    public IReadOnlyList<AlertLevelOption> Levels { get; } = [new(null, "Усі рівні"), new(AlertLevel.Warning, "Попередження"), new(AlertLevel.Critical, "Критичні")];
    public IReadOnlyList<AlertStatusOption> Statuses { get; } = [new(null, "Усі стани"), new(AlertStatus.Open, "Відкриті"), new(AlertStatus.Acknowledged, "Підтверджені"), new(AlertStatus.Resolved, "Вирішені")];
    public ObservableCollection<AlertItem> Alerts { get; } = [];
    public AlertsViewModel(IClientApi api, ClientSession session, INavigationService navigation) : base(navigation)
    {
        this.api = api; this.session = session; this.navigation = navigation;
        selectedLevel = Levels[0]; selectedStatus = Statuses[0]; Title = "Сповіщення";
    }
    public override Task InitializeAsync() => RefreshCommand.ExecuteAsync(null);
    [RelayCommand] private Task RefreshAsync() => RunAsync(async token => { Alerts.Clear(); HasMore = false; await LoadAsync(1, token); });
    [RelayCommand] private Task LoadMoreAsync() => HasMore ? RunAsync(token => LoadAsync(SelectedLevel.Value == appliedLevel && SelectedStatus.Value == appliedStatus ? page + 1 : 1, token)) : Task.CompletedTask;
    private async Task LoadAsync(int requestedPage, CancellationToken token)
    {
        var level = requestedPage == 1 ? SelectedLevel.Value : appliedLevel;
        var status = requestedPage == 1 ? SelectedStatus.Value : appliedStatus;
        var response = await api.GetAlertsAsync(requestedPage, level, status, token);
        token.ThrowIfCancellationRequested();
        if (requestedPage == 1) Alerts.Clear();
        foreach (var alert in response.Items)
        {
            if (Alerts.All(existing => existing.Alert.Id != alert.Id))
                Alerts.Add(new AlertItem(alert, alert.Status == AlertStatus.Open && CanManageAlerts()));
        }
        appliedLevel = level; appliedStatus = status;
        page = requestedPage; HasMore = response.Items.Count > 0 && Alerts.Count < response.TotalCount;
    }
    private bool CanManageAlerts() => session.Current?.User.Role is UserRole.Admin or UserRole.Operator;
    [RelayCommand]
    private Task AcknowledgeAsync(AlertItem item) => RunAsync(async token =>
    {
        if (!CanManageAlerts() || item.Alert.Status != AlertStatus.Open) { ErrorMessage = "Підтвердження доступне Admin/Operator лише для відкритих сповіщень."; return; }
        await api.AcknowledgeAsync(item.Alert.Id, token);
        await LoadAsync(1, token);
    });
    [RelayCommand] private Task OpenRoomAsync(AlertItem item) => navigation.GoToRoomDetailsAsync(item.Alert.RoomId);
}
