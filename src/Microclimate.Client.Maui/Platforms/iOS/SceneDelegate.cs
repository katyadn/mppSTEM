using Foundation;

namespace Microclimate.Client.Maui;

// Делегат віконної сцени iOS, зареєстрований для MAUI у UIKit.
[Register("SceneDelegate")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "UIKit/MAUI platform delegate registered by its native name.")]
public class SceneDelegate : MauiUISceneDelegate
{
}
