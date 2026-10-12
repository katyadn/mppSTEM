using CommunityToolkit.Mvvm.Input;
using Microclimate.Client.Maui.Services;

namespace Microclimate.Client.Maui.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly INavigationService navigationService;

    public SettingsViewModel(INavigationService navigationService)
    {
        this.navigationService = navigationService;
        Title = "Налаштування";
    }

    [RelayCommand]
    private Task LogoutAsync() => navigationService.GoToLoginAsync();
}
