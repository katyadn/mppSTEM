using Microclimate.Client.Core.Services;
using Microclimate.Client.Maui.Services;
using Microclimate.Client.Core.ViewModels;

namespace Microclimate.Client.Maui.Views;

// Сторінка сповіщень, яка прив’язує ViewModel і узгоджує її завантаження з життєвим циклом екрана.
public partial class AlertsPage : ContentPage
{
    // Модель стану та команд цієї сторінки.
    private readonly AlertsViewModel viewModel;
    private readonly PageLiveUpdates live;
    // Типізований доступ до команд сторінки для скомпільованих XAML-прив’язок.
    public AlertsViewModel ViewModel => viewModel;

    // Ініціалізує AlertsPage та його залежності для клієнтського сценарію.
    public AlertsPage(AlertsViewModel viewModel, ClientRealtime realtime, INavigationService navigation)
    {
        this.viewModel = viewModel;
        InitializeComponent();
        BindingContext = viewModel;
        live = new PageLiveUpdates(realtime, viewModel, navigation, () => Array.Empty<Guid>(), true, viewModel.RefreshLiveAsync);
    }
    // Після появи сторінки асинхронно ініціалізує її ViewModel.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        live.Start(Window);
        await viewModel.InitializeAsync();
    }

    // Скасовує запит ViewModel при виході зі сторінки.
    protected override void OnDisappearing()
    {
        live.Stop();
        viewModel.CancelPending();
        base.OnDisappearing();
    }
}
