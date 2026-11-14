using Foundation;

namespace Microclimate.Client.Maui;

// Платформний делегат UIKit, що створює спільний MAUI-застосунок.
[Register("AppDelegate")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "UIKit/MAUI platform delegate registered by its native name.")]
public class AppDelegate : MauiUIApplicationDelegate
{
	// Створює MAUI-застосунок зі спільною конфігурацією залежностей.
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
