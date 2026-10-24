namespace Microclimate.Client.Core.Services;

// Створює HTTP-транспорт із системною перевіркою TLS та вимкненими перенаправленнями.
public static class ClientTransport
{
    // Перенаправлення не повинно переносити пароль або токен до іншого сервера.
    // Створює HttpClient із тайм-аутом 15 секунд і вимкненими redirect, щоб не передавати облікові дані іншій адресі.
    public static HttpClient Create() => new(new HttpClientHandler { AllowAutoRedirect = false })
    {
        Timeout = TimeSpan.FromSeconds(15),
    };
}
