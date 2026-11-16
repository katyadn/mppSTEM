using Microsoft.Extensions.DependencyInjection;

namespace Microclimate.Client.Maui;

public partial class App : Application
{
    private readonly IServiceProvider services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        this.services = services;
        var settings = services.GetRequiredService<Microclimate.Client.Core.Services.IClientSettings>();
        UserAppTheme = settings.Theme switch
        {
            Microclimate.Client.Core.Services.ClientTheme.Light => AppTheme.Light,
            Microclimate.Client.Core.Services.ClientTheme.Dark => AppTheme.Dark,
            _ => AppTheme.Unspecified,
        };
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(services.GetRequiredService<AppShell>());
#if MACCATALYST
        var titleBar = new TitleBar { Title = "Мікроклімат", HeightRequest = 42 };
        titleBar.SetAppThemeColor(
            TitleBar.ForegroundColorProperty,
            (Color)Resources["TextLight"],
            (Color)Resources["TextDark"]
        );
        titleBar.SetAppThemeColor(
            VisualElement.BackgroundColorProperty,
            (Color)Resources["CanvasLight"],
            (Color)Resources["CanvasDark"]
        );
        window.TitleBar = titleBar;
#endif
        return window;
    }
}
