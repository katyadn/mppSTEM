using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microclimate.Client.Core.Presentation;
using Microclimate.Client.Core.Services;

namespace Microclimate.Client.Core.ViewModels;

// Завантажує сторінки кімнат без дублікатів і відкриває деталі вибраної кімнати.
public partial class DashboardViewModel : BaseViewModel
{
    // Клієнтський API для завантаження даних екрана.
    private readonly IClientApi api;
    // Сервіс переходів між екранами.
    private readonly INavigationService navigation;
    // Номер останньої успішно завантаженої сторінки.
    private int page;
    // Спостережуваний прапорець доступності наступної сторінки.
    [ObservableProperty] private bool hasMore;
    // Картки завантажених кімнат для спостережуваного списку.
    public ObservableCollection<RoomCard> Rooms { get; } = [];

    // Ініціалізує DashboardViewModel та його залежності для клієнтського сценарію.
    public DashboardViewModel(IClientApi api, INavigationService navigation) : base(navigation)
    {
        this.api = api; this.navigation = navigation; Title = "Огляд кімнат";
    }

    // Оновлює стан екрана під час його появи.
    public override Task InitializeAsync() => RefreshCommand.ExecuteAsync(null);
    // Очищає старі кімнати до запиту й завантажує першу сторінку.
    [RelayCommand] private Task RefreshAsync() => RunAsync(async token => { Rooms.Clear(); HasMore = false; await LoadAsync(1, token); });
    // За наявності результатів завантажує наступну сторінку.
    [RelayCommand] private Task LoadMoreAsync() => HasMore ? RunAsync(token => LoadAsync(page + 1, token)) : Task.CompletedTask;
    // Додає кімнати без повторення ідентифікаторів і перераховує наявність наступної сторінки.
    private async Task LoadAsync(int requestedPage, CancellationToken token)
    {
        var response = await api.GetRoomsAsync(requestedPage, token);
        token.ThrowIfCancellationRequested();
        if (requestedPage == 1) Rooms.Clear();
        foreach (var room in response.Items)
        {
            if (Rooms.All(existing => existing.Id != room.Id)) Rooms.Add(RoomCard.FromResponse(room));
        }
        page = requestedPage;
        HasMore = response.Items.Count > 0 && Rooms.Count < response.TotalCount;
    }
    // Зберігає завантажені сторінки й позицію списку під час фонових подій.
    public Task RefreshLiveAsync() => RunAsync(async token =>
    {
        var cards = new List<RoomCard>();
        var total = 0;
        for (var requested = 1; requested <= Math.Max(1, page); requested++)
        {
            var response = await api.GetRoomsAsync(requested, token);
            total = response.TotalCount;
            cards.AddRange(response.Items.Select(RoomCard.FromResponse));
            if (cards.Count >= total || response.Items.Count == 0) break;
        }
        token.ThrowIfCancellationRequested();
        var unique = cards.DistinctBy(card => card.Id).ToArray();
        for (var index = 0; index < unique.Length; index++)
        {
            if (index >= Rooms.Count) Rooms.Add(unique[index]);
            else if (Rooms[index] != unique[index]) Rooms[index] = unique[index];
        }
        while (Rooms.Count > unique.Length) Rooms.RemoveAt(Rooms.Count - 1);
        HasMore = Rooms.Count < total;
    });

    // Передає ідентифікатор і назву вибраної кімнати екрану деталей.
    [RelayCommand] private Task OpenSelectedRoomAsync(RoomCard room) => navigation.GoToRoomDetailsAsync(room.Id, room.Name);
}
