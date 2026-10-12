using CommunityToolkit.Mvvm.Input;
using Microclimate.Client.Maui.Services;

namespace Microclimate.Client.Maui.ViewModels;

public partial class RoomDetailsViewModel : BaseViewModel
{
    private readonly INavigationService navigationService;

    public RoomDetailsViewModel(INavigationService navigationService)
    {
        this.navigationService = navigationService;
        Title = "Деталі кімнати";
    }

    [RelayCommand]
    private Task GoBackAsync() => navigationService.GoBackAsync();
}
