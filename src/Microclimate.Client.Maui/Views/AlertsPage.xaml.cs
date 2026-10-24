using Microclimate.Client.Core.ViewModels;

namespace Microclimate.Client.Maui.Views;

public partial class AlertsPage : ContentPage
{
    private readonly AlertsViewModel viewModel;
    public AlertsViewModel ViewModel => viewModel;

    public AlertsPage(AlertsViewModel viewModel)
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
