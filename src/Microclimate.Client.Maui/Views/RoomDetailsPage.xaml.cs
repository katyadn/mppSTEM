using Microclimate.Client.Core.Services;
using Microclimate.Client.Core.ViewModels;
using Microclimate.Client.Maui.Services;

namespace Microclimate.Client.Maui.Views;

public partial class RoomDetailsPage : ContentPage, IQueryAttributable
{
    private readonly RoomDetailsViewModel viewModel;
    private readonly PageLiveUpdates live;

    public RoomDetailsPage(RoomDetailsViewModel viewModel, ClientRealtime realtime, INavigationService navigation)
    {
        InitializeComponent();
        BindingContext = viewModel;
        live = new PageLiveUpdates(
            realtime,
            viewModel,
            navigation,
            () => viewModel.RoomId is { } id ? new[] { id } : Array.Empty<Guid>(),
            false,
            viewModel.RefreshLiveAsync
        );
        this.viewModel = viewModel;
    }

    // Витягує RoomId і RoomName з параметрів Shell; порожній або некоректний ідентифікатор означає відсутність вибору.
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var roomId =
            query.TryGetValue("RoomId", out var id) && id is Guid value && value != Guid.Empty ? value : (Guid?)null;
        var name = query.TryGetValue("RoomName", out var title) ? title as string : null;
        viewModel.SelectRoom(roomId, name);
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
        viewModel.Thresholds.CancelPending();
        base.OnDisappearing();
    }
}
