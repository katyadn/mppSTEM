using CommunityToolkit.Mvvm.Input;
using Microclimate.Client.Maui.Services;

namespace Microclimate.Client.Maui.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly INavigationService navigationService;

    public LoginViewModel(INavigationService navigationService)
    {
        this.navigationService = navigationService;
        Title = "Вхід";
    }

    [RelayCommand]
    private Task LoginAsync() => navigationService.GoToDashboardAsync();
}
