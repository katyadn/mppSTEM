using System;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace Microclimate.Client.Maui;

// Платформна точка входу нативного клієнта.
class Program : MauiApplication
{
	// Створює MAUI-застосунок зі спільною конфігурацією залежностей.
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	// Передає аргументи запуску платформному циклу застосунку.
	static void Main(string[] args)
	{
		var app = new Program();
		app.Run(args);
	}
}
