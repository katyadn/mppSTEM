using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microclimate.Client.Core.Services;

namespace Microclimate.Client.Core.ViewModels;

// Керує адресою API, відображенням профілю, станом збереження сесії та виходом.
public partial class SettingsViewModel : BaseViewModel
{
    // Сховище кореневої адреси API.
    private readonly IClientSettings settings;
    // Поточна access-сесія та операції її очищення.
    private readonly ClientSession session;
    // Сервіс переходів між екранами.
    private readonly INavigationService navigation;
    // Редагована адреса API до перевірки й збереження.
    [ObservableProperty] private string serverAddress = string.Empty;
    // Спостережуване підтвердження успішного збереження налаштувань.
    [ObservableProperty] private string message = string.Empty;
    // Пошта й роль поточного користувача або повідомлення про відсутність входу.
    [ObservableProperty] private string profileText = string.Empty;
    // Пояснення, чи зберігається сесія між запусками або діє лише в пам’яті.
    [ObservableProperty] private string sessionText = string.Empty;

    // Ініціалізує SettingsViewModel та його залежності для клієнтського сценарію.
    public SettingsViewModel(IClientSettings settings, ClientSession session, INavigationService navigation) : base(navigation)
    { this.settings = settings; this.session = session; this.navigation = navigation; Title = "Налаштування"; }
    // Оновлює поля адреси, профілю та фактичного способу зберігання сесії.
    public override Task InitializeAsync()
    {
        ServerAddress = settings.ApiAddress; Message = string.Empty;
        ProfileText = session.Current is { } current ? $"{current.User.Email} · {current.User.Role}" : "Вхід не виконано";
        SessionText = session.Current is null ? string.Empty : session.IsPersistent ? "Сесію збережено на пристрої" : "Сесія діє до закриття застосунку";
        return Task.CompletedTask;
    }
    // Нормалізує адресу API; при зміні сервера очищає сесію та повертає до входу.
    [RelayCommand] private Task SaveAsync() => RunAsync(async _ =>
    {
        var normalized = ApiAddress.Normalize(ServerAddress);
        var changed = normalized != ApiAddress.Normalize(settings.ApiAddress);
        if (changed) await session.ClearAsync();
        settings.ApiAddress = normalized; ServerAddress = normalized; Message = "Адресу API збережено.";
        if (changed) await navigation.GoToLoginAsync();
    });
    // Видаляє поточну сесію та відкриває вхід.
    [RelayCommand] private Task LogoutAsync() => RunAsync(async _ => { await session.ClearAsync(); await navigation.GoToLoginAsync(); });
    // Відкриває екран входу.
    [RelayCommand] private Task GoToLoginAsync() => navigation.GoToLoginAsync();
}
