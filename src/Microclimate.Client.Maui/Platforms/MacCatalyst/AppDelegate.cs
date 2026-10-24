using Foundation;

namespace Microclimate.Client.Maui;

// Платформний делегат UIKit, що створює спільний MAUI-застосунок.
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	// Створює MAUI-застосунок зі спільною конфігурацією залежностей.
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
