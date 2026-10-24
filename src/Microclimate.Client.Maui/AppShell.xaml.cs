using Microsoft.Extensions.DependencyInjection;
using Microclimate.Client.Maui.Views;

namespace Microclimate.Client.Maui;

public partial class AppShell : Shell
{
    public AppShell(IServiceProvider services)
    {
        InitializeComponent();
        LoginContent.ContentTemplate = new DataTemplate(() => services.GetRequiredService<LoginPage>());
        DashboardContent.ContentTemplate = new DataTemplate(() => services.GetRequiredService<DashboardPage>());
        AlertsContent.ContentTemplate = new DataTemplate(() => services.GetRequiredService<AlertsPage>());
        SettingsContent.ContentTemplate = new DataTemplate(() => services.GetRequiredService<SettingsPage>());
        Routing.RegisterRoute("room-details", typeof(RoomDetailsPage));
        Routing.RegisterRoute("client-settings", typeof(SettingsPage));
    }
}
