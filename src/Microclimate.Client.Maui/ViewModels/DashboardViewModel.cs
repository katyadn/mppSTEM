using CommunityToolkit.Mvvm.Input;
using Microclimate.Client.Maui.Services;

namespace Microclimate.Client.Maui.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly INavigationService navigationService;

    public DashboardViewModel(INavigationService navigationService)
    {
        this.navigationService = navigationService;
        Title = "Огляд кімнат";
    }

    [RelayCommand]
    private Task OpenRoomAsync() => navigationService.GoToRoomDetailsAsync();
}
