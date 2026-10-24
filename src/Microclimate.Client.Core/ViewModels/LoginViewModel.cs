using System.Net.Mail;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microclimate.Client.Core.Services;

namespace Microclimate.Client.Core.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly IClientApi api;
    public LoginViewModel(IClientApi api, INavigationService navigation) : base(navigation)
    { this.api = api; Title = "Вхід"; }
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string password = string.Empty;

    public override Task InitializeAsync() => RunAsync(async token =>
    {
        if (await api.RestoreSessionAsync(token)) await Navigation.GoToDashboardAsync();
    }, false);

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

    [RelayCommand]
    private Task OpenSettingsAsync() => Navigation.GoToSettingsAsync();
}
