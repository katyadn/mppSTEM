using Microclimate.Client.Core.ViewModels;

namespace Microclimate.Client.Maui.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel viewModel;
    public DashboardViewModel ViewModel => viewModel;

    public DashboardPage(DashboardViewModel viewModel)
    {
        this.viewModel = viewModel;
        InitializeComponent();
        BindingContext = viewModel;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.InitializeAsync();
    }

    protected override void OnDisappearing()
    {
        viewModel.CancelPending();
        base.OnDisappearing();
    }
}
