using Microclimate.Client.Core.ViewModels;

namespace Microclimate.Client.Maui.Views;

// Сторінка налаштувань, яка прив’язує ViewModel і узгоджує її завантаження з життєвим циклом екрана.
public partial class SettingsPage : ContentPage
{
    // Модель стану та команд цієї сторінки.
    private readonly SettingsViewModel viewModel;

    // Ініціалізує SettingsPage та його залежності для клієнтського сценарію.
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        this.viewModel = viewModel;
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
