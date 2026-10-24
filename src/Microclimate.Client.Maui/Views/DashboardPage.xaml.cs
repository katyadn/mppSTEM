using Microclimate.Client.Core.ViewModels;

namespace Microclimate.Client.Maui.Views;

// Сторінка огляду кімнат, яка прив’язує ViewModel і узгоджує її завантаження з життєвим циклом екрана.
public partial class DashboardPage : ContentPage
{
    // Модель стану та команд цієї сторінки.
    private readonly DashboardViewModel viewModel;
    // Типізований доступ до команд сторінки для скомпільованих XAML-прив’язок.
    public DashboardViewModel ViewModel => viewModel;

    // Ініціалізує DashboardPage та його залежності для клієнтського сценарію.
    public DashboardPage(DashboardViewModel viewModel)
    {
        this.viewModel = viewModel;
        InitializeComponent();
        BindingContext = viewModel;
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
