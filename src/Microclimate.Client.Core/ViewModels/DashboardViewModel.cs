using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microclimate.Client.Core.Presentation;
using Microclimate.Client.Core.Services;

namespace Microclimate.Client.Core.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly IClientApi api;
    private readonly INavigationService navigation;
    private int page;
    [ObservableProperty] private bool hasMore;
    public ObservableCollection<RoomCard> Rooms { get; } = [];

    public DashboardViewModel(IClientApi api, INavigationService navigation) : base(navigation)
    {
        this.api = api; this.navigation = navigation; Title = "Огляд кімнат";
    }

    public override Task InitializeAsync() => RefreshCommand.ExecuteAsync(null);
    [RelayCommand] private Task RefreshAsync() => RunAsync(async token => { Rooms.Clear(); HasMore = false; await LoadAsync(1, token); });
    [RelayCommand] private Task LoadMoreAsync() => HasMore ? RunAsync(token => LoadAsync(page + 1, token)) : Task.CompletedTask;
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
    [RelayCommand] private Task OpenSelectedRoomAsync(RoomCard room) => navigation.GoToRoomDetailsAsync(room.Id, room.Name);
}
