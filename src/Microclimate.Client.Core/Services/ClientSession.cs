using System.Text.Json;
using Microclimate.Contracts.Auth;

namespace Microclimate.Client.Core.Services;

// Сховище серіалізованої access-сесії; реалізація повідомляє, чи вдалося зберегти її між запусками.
public interface ISessionStore
{
    // Читає серіалізовану сесію або повертає null, якщо вона недоступна.
    Task<string?> ReadAsync();
    // Намагається зберегти серіалізовану сесію між запусками.
    Task<bool> WriteAsync(string value);
    // Видаляє або робить недоступною збережену сесію.
    Task ClearAsync();
}

// Access-сесія конкретного сервера з терміном дії та профілем; не містить пароля або refresh-токена.
public sealed record StoredSession(string ServerAddress, string AccessToken, DateTimeOffset ExpiresAt, UserResponse User);

// Відновлює сесію один раз і очищає її при завершенні терміну дії або зміні сервера.
public sealed class ClientSession(ISessionStore store, TimeProvider clock)
{
    // Ознака завершеної спроби читання сховища, щоб не відновити вже очищений токен повторно.
    private bool restored;
    // Поточна сесія в пам’яті; null означає відсутність авторизації.
    public StoredSession? Current { get; private set; }
    // Чи підтвердило сховище збереження сесії між запусками застосунку.
    public bool IsPersistent { get; private set; }

    // Одноразово читає сесію; некоректний JSON очищає сховище.
    public async Task RestoreAsync()
    {
        if (restored) return;
        restored = true;
        try
        {
            var value = await store.ReadAsync().ConfigureAwait(false);
            Current = value is null ? null : JsonSerializer.Deserialize(value, ClientJsonContext.Default.StoredSession);
            IsPersistent = Current is not null;
        }
        catch (JsonException)
        {
            await ClearAsync().ConfigureAwait(false);
        }
    }

    // Повертає токен лише для того самого сервера й чинного терміну; непридатну сесію очищає.
    public async Task<string?> TokenAsync(string serverAddress)
    {
        await RestoreAsync().ConfigureAwait(false);
        if (Current is null) return null;
        if (string.IsNullOrWhiteSpace(Current.AccessToken) || Current.ExpiresAt <= clock.GetUtcNow() || Current.ServerAddress != serverAddress)
        {
            await ClearAsync().ConfigureAwait(false);
            return null;
        }

        return Current.AccessToken;
    }

    // Оновлює сесію в пам’яті та фіксує, чи вдалося постійне збереження.
    public async Task SaveAsync(StoredSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        Current = session;
        restored = true;
        IsPersistent = await store.WriteAsync(JsonSerializer.Serialize(session, ClientJsonContext.Default.StoredSession)).ConfigureAwait(false);
    }

    // Очищає сесію в пам’яті та у платформному сховищі.
    public async Task ClearAsync()
    {
        Current = null;
        IsPersistent = false;
        restored = true;
        await store.ClearAsync().ConfigureAwait(false);
    }
}
