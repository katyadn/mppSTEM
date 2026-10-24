using Microclimate.Client.Core.ViewModels;

namespace Microclimate.Client.Maui.Views;

// Сторінка сповіщень, яка прив’язує ViewModel і узгоджує її завантаження з життєвим циклом екрана.
public partial class AlertsPage : ContentPage
{
    // Модель стану та команд цієї сторінки.
    private readonly AlertsViewModel viewModel;
    // Типізований доступ до команд сторінки для скомпільованих XAML-прив’язок.
    public AlertsViewModel ViewModel => viewModel;

    // Ініціалізує AlertsPage та його залежності для клієнтського сценарію.
    public AlertsPage(AlertsViewModel viewModel)
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
