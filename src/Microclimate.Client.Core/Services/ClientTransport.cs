namespace Microclimate.Client.Core.Services;

public static class ClientTransport
{
    // Перенаправлення не повинно переносити пароль або токен до іншого сервера.
    public static HttpClient Create() => new(new HttpClientHandler { AllowAutoRedirect = false })
    {
        Timeout = TimeSpan.FromSeconds(15),
    };
}
