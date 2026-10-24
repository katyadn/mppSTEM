using Microclimate.Client.Core.Services;

namespace Microclimate.Client.Maui.Services;

// Зберігає адресу API у Preferences; для Android за замовчуванням використовує адресу емулятора.
public sealed class PlatformClientSettings : IClientSettings
{
    // Коренева адреса сервера; ClientApi додає до неї api/v1/.
    public string ApiAddress
    {
        get => Preferences.Default.Get("api-address", DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5195/" : "http://localhost:5195/");
        set => Preferences.Default.Set("api-address", Microclimate.Client.Core.Services.ApiAddress.Normalize(value));
    }
}

// Зберігає сесію у SecureStorage; при недоступності сховища не записує токен відкритим текстом.
public sealed class SecureSessionStore : ISessionStore
{
    // Версійований ключ access-сесії у системному захищеному сховищі.
    private const string Key = "microclimate-session-v1";
    // Читає сесію лише за прапорця session-enabled; недоступність SecureStorage означає потребу нового входу.
    public async Task<string?> ReadAsync()
    {
        if (!Preferences.Default.Get("session-enabled", false)) return null;
        try { return await SecureStorage.Default.GetAsync(Key); }
        catch (Exception) { return null; } // Пошкоджене/недоступне системне сховище: потрібен новий вхід.
    }
    // Пише сесію в SecureStorage; при помилці повертає false без збереження токена у Preferences.
    public async Task<bool> WriteAsync(string value)
    {
        Preferences.Default.Set("session-enabled", false);
        try { await SecureStorage.Default.SetAsync(Key, value); Preferences.Default.Set("session-enabled", true); return true; }
        catch (Exception) { return false; } // Токен залишається в пам’яті; plaintext fallback відсутній.
    }
    // Вимикає відновлення сесії та намагається видалити захищений запис.
    public Task ClearAsync()
    {
        Preferences.Default.Set("session-enabled", false);
        try { SecureStorage.Default.Remove(Key); }
        catch (Exception) { /* Сесію в пам’яті вже видалив ClientSession. */ }
        return Task.CompletedTask;
    }
}
