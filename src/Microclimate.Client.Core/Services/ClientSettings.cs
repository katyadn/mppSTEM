namespace Microclimate.Client.Core.Services;

public interface IClientSettings
{
    string ApiAddress { get; set; }
}

public static class ApiAddress
{
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
