using Microclimate.Client.Maui.ViewModels;

namespace Microclimate.Client.Maui.Views;

public partial class AlertsPage : ContentPage
{
    public AlertsPage(AlertsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
