using Android.App;
using Android.Runtime;

namespace Microclimate.Client.Maui;

// З’єднує життєвий цикл Android Application зі спільним MAUI-застосунком.
[Application]
public class MainApplication : MauiApplication
{
	// Передає нативний дескриптор і правила володіння базовому Android Application.
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}

	// Створює MAUI-застосунок зі спільною конфігурацією залежностей.
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
