using Microclimate.Client.Core.ViewModels;

namespace Microclimate.Client.Maui.Views;

// Сторінка аналітики кімнати, яка прив’язує ViewModel і узгоджує її завантаження з життєвим циклом екрана.
public partial class RoomDetailsPage : ContentPage, IQueryAttributable
{
    // Модель стану та команд цієї сторінки.
    private readonly RoomDetailsViewModel viewModel;

    // Ініціалізує RoomDetailsPage та його залежності для клієнтського сценарію.
    public RoomDetailsPage(RoomDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        this.viewModel = viewModel;
    }

    // Витягує RoomId і RoomName з параметрів Shell; порожній або некоректний ідентифікатор означає відсутність вибору.
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var roomId = query.TryGetValue("RoomId", out var id) && id is Guid value && value != Guid.Empty
            ? value : (Guid?)null;
        var name = query.TryGetValue("RoomName", out var title) ? title as string : null;
        viewModel.SelectRoom(roomId, name);
    }
    // Після появи сторінки асинхронно ініціалізує її ViewModel.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.InitializeAsync();
    }

    // Скасовує запит ViewModel при виході зі сторінки.
    protected override void OnDisappearing()
    {
        viewModel.CancelPending();
        base.OnDisappearing();
    }
}
