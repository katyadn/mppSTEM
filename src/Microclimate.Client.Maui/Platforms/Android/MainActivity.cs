using Android.App;
using Android.Content.PM;
using Android.OS;

namespace Microclimate.Client.Maui;

// Стартова Android Activity зі splash-темою та обробкою змін конфігурації MAUI.
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
}
