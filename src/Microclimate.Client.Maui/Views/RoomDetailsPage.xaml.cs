using Microclimate.Client.Maui.ViewModels;

namespace Microclimate.Client.Maui.Views;

public partial class RoomDetailsPage : ContentPage
{
    public RoomDetailsPage(RoomDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
