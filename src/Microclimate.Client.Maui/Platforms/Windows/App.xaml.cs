using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Microclimate.Client.Maui.WinUI;

// Точка входу Windows WinUI, що підключає спільну конфігурацію MAUI.
public partial class App : MauiWinUIApplication
{
	// Ініціалізує XAML-ресурси Windows-застосунку перед створенням MAUI.
	public App()
	{
		this.InitializeComponent();
	}

	// Створює MAUI-застосунок зі спільною конфігурацією залежностей.
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
