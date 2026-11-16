using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microclimate.Client.Core.Presentation;
using Microclimate.Client.Core.Services;
using Microclimate.Contracts.Common;

namespace Microclimate.Client.Core.ViewModels;

// Пункт фільтра рівня сповіщень; null означає всі рівні.
public sealed record AlertLevelOption(AlertLevel? Value, string Name);

// Пункт фільтра стану сповіщень; null означає всі стани.
public sealed record AlertStatusOption(AlertStatus? Value, string Name);

// Керує фільтрами, пагінацією та підтвердженням відкритих сповіщень відповідно до ролі.
public partial class AlertsViewModel : BaseViewModel
{
    private readonly IClientApi api;

    private readonly ClientSession session;

    private readonly INavigationService navigation;

    // Номер останньої успішно завантаженої сторінки.
    private int page;

    // Рівень, із яким завантажено поточний список, для послідовної пагінації.
    private AlertLevel? appliedLevel;

    // Стан, із яким завантажено поточний список, для послідовної пагінації.
    private AlertStatus? appliedStatus;

    [ObservableProperty]
    private bool hasMore;

    [ObservableProperty]
    private AlertLevelOption selectedLevel;

    [ObservableProperty]
    private AlertStatusOption selectedStatus;

    public IReadOnlyList<AlertLevelOption> Levels { get; } =
    [new(null, "Усі рівні"), new(AlertLevel.Warning, "Попередження"), new(AlertLevel.Critical, "Критичні")];

    public IReadOnlyList<AlertStatusOption> Statuses { get; } =
    [
        new(null, "Усі стани"),
        new(AlertStatus.Open, "Відкриті"),
        new(AlertStatus.Acknowledged, "Підтверджені"),
        new(AlertStatus.Resolved, "Вирішені"),
    ];

    public ObservableCollection<AlertItem> Alerts { get; } = [];

    public AlertsViewModel(IClientApi api, ClientSession session, INavigationService navigation)
        : base(navigation)
    {
        this.api = api;
        this.session = session;
        this.navigation = navigation;
        selectedLevel = Levels[0];
        selectedStatus = Statuses[0];
        Title = "Сповіщення";
    }

    public override Task InitializeAsync() => RefreshCommand.ExecuteAsync(null);

    // Очищає попередні результати й завантажує першу сторінку за вибраними фільтрами.
    [RelayCommand]
    private Task RefreshAsync() =>
        RunAsync(async token =>
        {
            Alerts.Clear();
            HasMore = false;
            await LoadAsync(1, token);
        });

    // Завантажує наступну сторінку або починає з першої, якщо фільтри змінилися.
    [RelayCommand]
    private Task LoadMoreAsync() =>
        HasMore
            ? RunAsync(token =>
                LoadAsync(
                    SelectedLevel.Value == appliedLevel && SelectedStatus.Value == appliedStatus ? page + 1 : 1,
                    token
                )
            )
            : Task.CompletedTask;

    // Завантажує сторінку без дублікатів і фіксує застосовані фільтри; скасований запит не змінює список.
    private async Task LoadAsync(int requestedPage, CancellationToken token)
    {
        var level = requestedPage == 1 ? SelectedLevel.Value : appliedLevel;
        var status = requestedPage == 1 ? SelectedStatus.Value : appliedStatus;
        var response = await api.GetAlertsAsync(requestedPage, level, status, token);
        token.ThrowIfCancellationRequested();
        if (requestedPage == 1)
            Alerts.Clear();
        foreach (var alert in response.Items)
        {
            if (Alerts.All(existing => existing.Alert.Id != alert.Id))
                Alerts.Add(new AlertItem(alert, alert.Status == AlertStatus.Open && CanManageAlerts()));
        }
        appliedLevel = level;
        appliedStatus = status;
        page = requestedPage;
        HasMore = response.Items.Count > 0 && Alerts.Count < response.TotalCount;
    }

    public Task RefreshLiveAsync() =>
        RunAsync(async token =>
        {
            var items = new List<AlertItem>();
            var total = 0;
            var pages =
                SelectedLevel.Value == appliedLevel && SelectedStatus.Value == appliedStatus ? Math.Max(1, page) : 1;
            var level = SelectedLevel.Value;
            var status = SelectedStatus.Value;
            for (var requested = 1; requested <= pages; requested++)
            {
                var response = await api.GetAlertsAsync(requested, level, status, token);
                total = response.TotalCount;
                items.AddRange(
                    response.Items.Select(alert => new AlertItem(
                        alert,
                        alert.Status == AlertStatus.Open && CanManageAlerts()
                    ))
                );
                if (items.Count >= total || response.Items.Count == 0)
                    break;
            }
            token.ThrowIfCancellationRequested();
            var unique = items.DistinctBy(item => item.Alert.Id).ToArray();
            for (var index = 0; index < unique.Length; index++)
            {
                if (index >= Alerts.Count)
                    Alerts.Add(unique[index]);
                else if (Alerts[index] != unique[index])
                    Alerts[index] = unique[index];
            }
            while (Alerts.Count > unique.Length)
                Alerts.RemoveAt(Alerts.Count - 1);
            appliedLevel = level;
            appliedStatus = status;
            page = pages;
            HasMore = Alerts.Count < total;
        });

    // Дозволяє кнопку підтвердження для Admin та Operator; сервер додатково перевіряє права.
    private bool CanManageAlerts() => session.Current?.User.Role is UserRole.Admin or UserRole.Operator;

    // Для Admin або Operator підтверджує лише відкрите сповіщення й перезавантажує першу сторінку.
    [RelayCommand]
    private Task AcknowledgeAsync(AlertItem item) =>
        RunAsync(async token =>
        {
            if (!CanManageAlerts() || item.Alert.Status != AlertStatus.Open)
            {
                ErrorMessage = "Підтвердження доступне Admin/Operator лише для відкритих сповіщень.";
                return;
            }
            await api.AcknowledgeAsync(item.Alert.Id, token);
            await LoadAsync(1, token);
        });

    [RelayCommand]
    private Task OpenRoomAsync(AlertItem item) => navigation.GoToRoomDetailsAsync(item.Alert.RoomId);
}
