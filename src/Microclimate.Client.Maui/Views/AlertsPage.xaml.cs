using Microclimate.Client.Core.Services;
using Microclimate.Client.Core.ViewModels;
using Microclimate.Client.Maui.Services;

namespace Microclimate.Client.Maui.Views;

public partial class AlertsPage : ContentPage
{
    private readonly AlertsViewModel viewModel;
    private readonly PageLiveUpdates live;

    // Типізований доступ до команд сторінки для скомпільованих XAML-прив’язок.
    public AlertsViewModel ViewModel => viewModel;

    public AlertsPage(AlertsViewModel viewModel, ClientRealtime realtime, INavigationService navigation)
    {
        this.viewModel = viewModel;
        InitializeComponent();
        BindingContext = viewModel;
        live = new PageLiveUpdates(
            realtime,
            viewModel,
            navigation,
            () => Array.Empty<Guid>(),
            true,
            viewModel.RefreshLiveAsync
        );
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        live.Start(Window);
        await viewModel.InitializeAsync();
    }

    protected override void OnDisappearing()
    {
        live.Stop();
        viewModel.CancelPending();
        base.OnDisappearing();
    }
}
