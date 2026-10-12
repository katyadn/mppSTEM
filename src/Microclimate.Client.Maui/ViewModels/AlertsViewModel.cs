using CommunityToolkit.Mvvm.Input;

namespace Microclimate.Client.Maui.ViewModels;

public partial class AlertsViewModel : BaseViewModel
{
    public AlertsViewModel()
    {
        Title = "Сповіщення";
    }

    // TODO: завантаження реальних сповіщень після погодження API на Етапі 1.
    [RelayCommand]
    private void Refresh() => Title = "Сповіщення";
}
