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
    }

    // Створює вікно з новим екземпляром AppShell із DI-контейнера.
    protected override Window CreateWindow(IActivationState? activationState) =>
        new(services.GetRequiredService<AppShell>());
}
