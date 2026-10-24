using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microclimate.Client.Core.Services;

namespace Microclimate.Client.Core.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly IClientSettings settings;
    private readonly ClientSession session;
    private readonly INavigationService navigation;
    [ObservableProperty] private string serverAddress = string.Empty;
    [ObservableProperty] private string message = string.Empty;
    [ObservableProperty] private string profileText = string.Empty;
    [ObservableProperty] private string sessionText = string.Empty;

    public SettingsViewModel(IClientSettings settings, ClientSession session, INavigationService navigation) : base(navigation)
    { this.settings = settings; this.session = session; this.navigation = navigation; Title = "Налаштування"; }
    public override Task InitializeAsync()
    {
        ServerAddress = settings.ApiAddress; Message = string.Empty;
        ProfileText = session.Current is { } current ? $"{current.User.Email} · {current.User.Role}" : "Вхід не виконано";
        SessionText = session.Current is null ? string.Empty : session.IsPersistent ? "Сесію збережено на пристрої" : "Сесія діє до закриття застосунку";
        return Task.CompletedTask;
    }
    [RelayCommand] private Task SaveAsync() => RunAsync(async _ =>
    {
        var normalized = ApiAddress.Normalize(ServerAddress);
        var changed = normalized != ApiAddress.Normalize(settings.ApiAddress);
        if (changed) await session.ClearAsync();
        settings.ApiAddress = normalized; ServerAddress = normalized; Message = "Адресу API збережено.";
        if (changed) await navigation.GoToLoginAsync();
    });
    [RelayCommand] private Task LogoutAsync() => RunAsync(async _ => { await session.ClearAsync(); await navigation.GoToLoginAsync(); });
    [RelayCommand] private Task GoToLoginAsync() => navigation.GoToLoginAsync();
}
