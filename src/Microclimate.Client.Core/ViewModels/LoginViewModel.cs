using System.Net.Mail;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microclimate.Client.Core.Services;

namespace Microclimate.Client.Core.ViewModels;

// Перевіряє форму входу, відновлює сесію та очищає пароль після спроби авторизації.
public partial class LoginViewModel : BaseViewModel
{
    // Клієнтський API для завантаження даних екрана.
    private readonly IClientApi api;
    // Ініціалізує LoginViewModel та його залежності для клієнтського сценарію.
    public LoginViewModel(IClientApi api, INavigationService navigation) : base(navigation)
    { this.api = api; Title = "Вхід"; }
    // Спостережувана адреса пошти з форми входу.
    [ObservableProperty] private string email = string.Empty;
    // Пароль лише в пам’яті форми; очищається після спроби входу.
    [ObservableProperty] private string password = string.Empty;

    // Намагається відновити чинну сесію та відкрити огляд без повторного введення пароля.
    public override Task InitializeAsync() => RunAsync(async token =>
    {
        if (await api.RestoreSessionAsync(token)) await Navigation.GoToDashboardAsync();
    }, false);

    // Перевіряє email і пароль, виконує вхід та очищає пароль у finally незалежно від результату.
    [RelayCommand]
    private Task LoginAsync() => RunAsync(async token =>
    {
        var address = Email.Trim();
        if (!MailAddress.TryCreate(address, out var parsed) || parsed.Address != address || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Введіть коректну електронну пошту та пароль.";
            return;
        }
        try
        {
            await api.LoginAsync(address, Password, token);
            await Navigation.GoToDashboardAsync();
        }
        finally { Password = string.Empty; }
    }, false);

    // Відкриває налаштування адреси сервера з екрана входу.
    [RelayCommand]
    private Task OpenSettingsAsync() => Navigation.GoToSettingsAsync();
}
