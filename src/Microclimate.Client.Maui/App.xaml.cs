using Microsoft.Extensions.DependencyInjection;

namespace Microclimate.Client.Maui;

public partial class App : Application
{
    private readonly IServiceProvider services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        this.services = services;
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(services.GetRequiredService<AppShell>());
}
