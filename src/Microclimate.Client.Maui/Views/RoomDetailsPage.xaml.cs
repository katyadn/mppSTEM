using Microclimate.Client.Core.ViewModels;

namespace Microclimate.Client.Maui.Views;

public partial class RoomDetailsPage : ContentPage, IQueryAttributable
{
    private readonly RoomDetailsViewModel viewModel;

    public RoomDetailsPage(RoomDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        this.viewModel = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var roomId = query.TryGetValue("RoomId", out var id) && id is Guid value && value != Guid.Empty
            ? value : (Guid?)null;
        var name = query.TryGetValue("RoomName", out var title) ? title as string : null;
        viewModel.SelectRoom(roomId, name);
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
