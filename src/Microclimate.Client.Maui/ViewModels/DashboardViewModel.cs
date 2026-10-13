using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Microclimate.Client.Core.Presentation;
using Microclimate.Client.Maui.Services;
using Microclimate.Contracts.Rooms;

namespace Microclimate.Client.Maui.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly INavigationService navigationService;

    public DashboardViewModel(INavigationService navigationService)
    {
        this.navigationService = navigationService;
        Title = "Огляд кімнат";
    }

    public ObservableCollection<RoomCard> Rooms { get; } = [];

    public void ApplyRooms(IEnumerable<RoomSummaryResponse> rooms)
    {
        ArgumentNullException.ThrowIfNull(rooms);
        var cards = rooms.Select(RoomCard.FromResponse).ToArray();
        Rooms.Clear();
        foreach (var card in cards)
        {
            Rooms.Add(card);
        }
    }

    [RelayCommand]
    private Task OpenRoomAsync() => navigationService.GoToRoomDetailsAsync();

    [RelayCommand]
    private Task OpenSelectedRoomAsync(RoomCard room) => navigationService.GoToRoomDetailsAsync(room.Id, room.Name);
}
