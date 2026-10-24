using ObjCRuntime;
using UIKit;

namespace Microclimate.Client.Maui;

// Платформна точка входу нативного клієнта.
public class Program
{
	// This is the main entry point of the application.
	// Передає аргументи запуску платформному циклу застосунку.
	static void Main(string[] args)
	{
		// if you want to use a different Application Delegate class from "AppDelegate"
		// you can specify it here.
		UIApplication.Main(args, null, typeof(AppDelegate));
	}
}
