using Microsoft.Extensions.DependencyInjection;

namespace Microclimate.Client.Maui;

// Кореневий застосунок MAUI, який створює вікно з оболонкою із DI-контейнера.
public partial class App : Application
{
    // DI-контейнер для створення оболонки нового вікна.
    private readonly IServiceProvider services;

    // Ініціалізує App та його залежності для клієнтського сценарію.
    public App(IServiceProvider services)
    {
        InitializeComponent();
        this.services = services;
        var settings = services.GetRequiredService<Microclimate.Client.Core.Services.IClientSettings>();
        UserAppTheme = settings.Theme switch { Microclimate.Client.Core.Services.ClientTheme.Light => AppTheme.Light, Microclimate.Client.Core.Services.ClientTheme.Dark => AppTheme.Dark, _ => AppTheme.Unspecified };
    }

    // Створює вікно з новим екземпляром AppShell із DI-контейнера.
    protected override Window CreateWindow(IActivationState? activationState) =>
        new(services.GetRequiredService<AppShell>());
}
