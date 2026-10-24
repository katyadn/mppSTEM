namespace Microclimate.Client.Core.Services;

// Налаштування кореневої адреси API, незалежні від платформного сховища.
public interface IClientSettings
{
    // Коренева адреса сервера; ClientApi додає до неї api/v1/.
    string ApiAddress { get; set; }
}

// Перевіряє адресу сервера: HTTPS для віддалених вузлів, HTTP лише для локальної розробки.
public static class ApiAddress
{
    // Перевіряє абсолютну адресу без credentials, query та fragment; додає завершальний слеш.
    public static string Normalize(string address)
    {
        if (!Uri.TryCreate(address?.Trim(), UriKind.Absolute, out var uri) ||
            !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) ||
            (uri.Scheme != Uri.UriSchemeHttps && !(uri.Scheme == Uri.UriSchemeHttp && (uri.IsLoopback || uri.Host == "10.0.2.2"))))
        {
            throw new ArgumentException("Вкажіть HTTPS-адресу API. HTTP дозволений лише для локальної розробки.");
        }

        return uri.AbsoluteUri.TrimEnd('/') + "/";
    }
}
