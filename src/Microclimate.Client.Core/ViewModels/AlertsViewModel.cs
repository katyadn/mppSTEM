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
    // Клієнтський API для завантаження даних екрана.
    private readonly IClientApi api;
    // Поточна access-сесія та операції її очищення.
    private readonly ClientSession session;
    // Сервіс переходів між екранами.
    private readonly INavigationService navigation;
    // Номер останньої успішно завантаженої сторінки.
    private int page;
    // Рівень, із яким завантажено поточний список, для послідовної пагінації.
    private AlertLevel? appliedLevel;
    // Стан, із яким завантажено поточний список, для послідовної пагінації.
    private AlertStatus? appliedStatus;
    // Спостережуваний прапорець доступності наступної сторінки.
    [ObservableProperty] private bool hasMore;
    // Спостережуваний вибір рівня для наступного оновлення списку.
    [ObservableProperty] private AlertLevelOption selectedLevel;
    // Спостережуваний вибір стану для наступного оновлення списку.
    [ObservableProperty] private AlertStatusOption selectedStatus;
    // Доступні рівні фільтра, включно з варіантом усіх рівнів.
    public IReadOnlyList<AlertLevelOption> Levels { get; } = [new(null, "Усі рівні"), new(AlertLevel.Warning, "Попередження"), new(AlertLevel.Critical, "Критичні")];
    // Доступні стани фільтра, включно з варіантом усіх станів.
    public IReadOnlyList<AlertStatusOption> Statuses { get; } = [new(null, "Усі стани"), new(AlertStatus.Open, "Відкриті"), new(AlertStatus.Acknowledged, "Підтверджені"), new(AlertStatus.Resolved, "Вирішені")];
    // Завантажені сповіщення з підготовленими текстами та дозволами кнопок.
    public ObservableCollection<AlertItem> Alerts { get; } = [];
    // Ініціалізує AlertsViewModel та його залежності для клієнтського сценарію.
    public AlertsViewModel(IClientApi api, ClientSession session, INavigationService navigation) : base(navigation)
    {
        this.api = api; this.session = session; this.navigation = navigation;
        selectedLevel = Levels[0]; selectedStatus = Statuses[0]; Title = "Сповіщення";
    }
    // Оновлює стан екрана під час його появи.
    public override Task InitializeAsync() => RefreshCommand.ExecuteAsync(null);
    // Очищає попередні результати й завантажує першу сторінку за вибраними фільтрами.
    [RelayCommand] private Task RefreshAsync() => RunAsync(async token => { Alerts.Clear(); HasMore = false; await LoadAsync(1, token); });
    // Завантажує наступну сторінку або починає з першої, якщо фільтри змінилися.
    [RelayCommand] private Task LoadMoreAsync() => HasMore ? RunAsync(token => LoadAsync(SelectedLevel.Value == appliedLevel && SelectedStatus.Value == appliedStatus ? page + 1 : 1, token)) : Task.CompletedTask;
    // Завантажує сторінку без дублікатів і фіксує застосовані фільтри; скасований запит не змінює список.
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
    // Дозволяє кнопку підтвердження для Admin та Operator; сервер додатково перевіряє права.
    private bool CanManageAlerts() => session.Current?.User.Role is UserRole.Admin or UserRole.Operator;
    // Для Admin або Operator підтверджує лише відкрите сповіщення й перезавантажує першу сторінку.
    [RelayCommand]
    private Task AcknowledgeAsync(AlertItem item) => RunAsync(async token =>
    {
        if (!CanManageAlerts() || item.Alert.Status != AlertStatus.Open) { ErrorMessage = "Підтвердження доступне Admin/Operator лише для відкритих сповіщень."; return; }
        await api.AcknowledgeAsync(item.Alert.Id, token);
        await LoadAsync(1, token);
    });
    // Відкриває кімнату, до якої належить вибране сповіщення.
    [RelayCommand] private Task OpenRoomAsync(AlertItem item) => navigation.GoToRoomDetailsAsync(item.Alert.RoomId);
}
