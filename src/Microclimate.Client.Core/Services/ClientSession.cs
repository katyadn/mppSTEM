using System.Text.Json;
using Microclimate.Contracts.Auth;

namespace Microclimate.Client.Core.Services;

public interface ISessionStore
{
    Task<string?> ReadAsync();
    Task<bool> WriteAsync(string value);
    Task ClearAsync();
}

public sealed record StoredSession(string ServerAddress, string AccessToken, DateTimeOffset ExpiresAt, UserResponse User);

public sealed class ClientSession(ISessionStore store, TimeProvider clock)
{
    private bool restored;
    public StoredSession? Current { get; private set; }
    public bool IsPersistent { get; private set; }

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

    public async Task SaveAsync(StoredSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        Current = session;
        restored = true;
        IsPersistent = await store.WriteAsync(JsonSerializer.Serialize(session, ClientJsonContext.Default.StoredSession)).ConfigureAwait(false);
    }

    public async Task ClearAsync()
    {
        Current = null;
        IsPersistent = false;
        restored = true;
        await store.ClearAsync().ConfigureAwait(false);
    }
}
