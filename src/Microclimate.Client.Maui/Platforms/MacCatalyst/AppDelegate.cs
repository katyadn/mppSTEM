using Foundation;

namespace Microclimate.Client.Maui;

[Register("AppDelegate")]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "UIKit/MAUI platform delegate registered by its native name."
)]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
