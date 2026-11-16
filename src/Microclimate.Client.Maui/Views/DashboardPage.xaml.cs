using Microclimate.Client.Core.Services;
using Microclimate.Client.Core.ViewModels;
using Microclimate.Client.Maui.Services;

namespace Microclimate.Client.Maui.Views;

// Сторінка огляду кімнат, яка прив’язує ViewModel і узгоджує її завантаження з життєвим циклом екрана.
public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel viewModel;
    private readonly PageLiveUpdates live;

    // Типізований доступ до команд сторінки для скомпільованих XAML-прив’язок.
    public DashboardViewModel ViewModel => viewModel;

    public DashboardPage(DashboardViewModel viewModel, ClientRealtime realtime, INavigationService navigation)
    {
        this.viewModel = viewModel;
        InitializeComponent();
        BindingContext = viewModel;
        live = new PageLiveUpdates(
            realtime,
            viewModel,
            navigation,
            () => viewModel.Rooms.Select(room => room.Id).ToArray(),
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
