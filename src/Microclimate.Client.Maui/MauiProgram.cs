using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microclimate.Client.Maui.Services;
using Microclimate.Client.Maui.ViewModels;
using Microclimate.Client.Maui.Views;

namespace Microclimate.Client.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>().ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
        });

        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddTransient<AppShell>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<RoomDetailsViewModel>();
        builder.Services.AddTransient<AlertsViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<RoomDetailsPage>();
        builder.Services.AddTransient<AlertsPage>();
        builder.Services.AddTransient<SettingsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
