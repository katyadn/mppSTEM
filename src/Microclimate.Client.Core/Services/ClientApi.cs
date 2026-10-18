using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microclimate.Contracts.Alerts;
using Microclimate.Contracts.Analytics;
using Microclimate.Contracts.Auth;
using Microclimate.Contracts.Common;
using Microclimate.Contracts.Rooms;

namespace Microclimate.Client.Core.Services;

public sealed class ClientApiException(string message, HttpStatusCode? statusCode = null) : Exception(message)
{
    public HttpStatusCode? StatusCode { get; } = statusCode;
}

public interface IClientApi
{
    Task LoginAsync(string email, string password, CancellationToken cancellationToken);
    Task<bool> RestoreSessionAsync(CancellationToken cancellationToken);
    Task<PagedResponse<RoomSummaryResponse>> GetRoomsAsync(int page, CancellationToken cancellationToken);
    Task<RoomResponse> GetRoomAsync(Guid roomId, CancellationToken cancellationToken);
    Task<MetricSummaryResponse> GetSummaryAsync(Guid roomId, MetricType metric, DateTimeOffset from, DateTimeOffset until, CancellationToken cancellationToken);
    Task<IReadOnlyList<SeriesPointResponse>> GetSeriesAsync(Guid roomId, MetricType metric, DateTimeOffset from, DateTimeOffset until, CancellationToken cancellationToken);
    Task<ComfortIndexResponse> GetComfortAsync(Guid roomId, CancellationToken cancellationToken);
    Task<PagedResponse<AlertResponse>> GetAlertsAsync(int page, AlertLevel? level, AlertStatus? status, CancellationToken cancellationToken);
    Task AcknowledgeAsync(Guid alertId, CancellationToken cancellationToken);
}

public sealed class ClientApi(HttpClient http, IClientSettings settings, ClientSession session, TimeProvider clock) : IClientApi
{
    public async Task LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        var server = ApiAddress.Normalize(settings.ApiAddress);
        await session.ClearAsync().ConfigureAwait(false);
        using var response = await SendAsync(HttpMethod.Post, "auth/login", cancellationToken,
            new LoginRequest(email, password), server: server, anonymous: true).ConfigureAwait(false);
        var tokens = await ReadAsync<TokenResponse>(response, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(tokens.AccessToken) || tokens.ExpiresIn is <= 0 or > 86_400 ||
            !string.Equals(tokens.TokenType, "Bearer", StringComparison.OrdinalIgnoreCase))
            throw new ClientApiException("Сервер повернув некоректну сесію.");
        using var me = await SendAsync(HttpMethod.Get, "auth/me", cancellationToken, server: server,
            explicitToken: tokens.AccessToken).ConfigureAwait(false);
        var user = await ReadAsync<UserResponse>(me, cancellationToken).ConfigureAwait(false);
        if (server != ApiAddress.Normalize(settings.ApiAddress)) throw new ClientApiException("Адреса API змінилася. Увійдіть знову.");
        await session.SaveAsync(new StoredSession(server, tokens.AccessToken, clock.GetUtcNow().AddSeconds(tokens.ExpiresIn), user)).ConfigureAwait(false);
    }

    public async Task<bool> RestoreSessionAsync(CancellationToken cancellationToken)
    {
        var server = ApiAddress.Normalize(settings.ApiAddress);
        if (await session.TokenAsync(server).ConfigureAwait(false) is null) return false;
        using var response = await SendAsync(HttpMethod.Get, "auth/me", cancellationToken).ConfigureAwait(false);
        var user = await ReadAsync<UserResponse>(response, cancellationToken).ConfigureAwait(false);
        if (session.Current is { } current) await session.SaveAsync(current with { User = user }).ConfigureAwait(false);
        return true;
    }

    public Task<PagedResponse<RoomSummaryResponse>> GetRoomsAsync(int page, CancellationToken cancellationToken) =>
        GetAsync<PagedResponse<RoomSummaryResponse>>($"rooms?page={page}&pageSize=20", cancellationToken);
    public Task<RoomResponse> GetRoomAsync(Guid roomId, CancellationToken cancellationToken) => GetAsync<RoomResponse>($"rooms/{roomId:D}", cancellationToken);
    public Task<MetricSummaryResponse> GetSummaryAsync(Guid roomId, MetricType metric, DateTimeOffset from, DateTimeOffset until, CancellationToken cancellationToken) =>
        GetAsync<MetricSummaryResponse>($"rooms/{roomId:D}/analytics/summary?{PeriodQuery(metric, from, until)}", cancellationToken);
    public async Task<IReadOnlyList<SeriesPointResponse>> GetSeriesAsync(Guid roomId, MetricType metric, DateTimeOffset from, DateTimeOffset until, CancellationToken cancellationToken) =>
        await GetAsync<List<SeriesPointResponse>>($"rooms/{roomId:D}/analytics/series?{PeriodQuery(metric, from, until)}&interval={((until - from).TotalDays <= 1 ? "hour" : "day")}", cancellationToken).ConfigureAwait(false);
    public Task<ComfortIndexResponse> GetComfortAsync(Guid roomId, CancellationToken cancellationToken) => GetAsync<ComfortIndexResponse>($"rooms/{roomId:D}/analytics/comfort", cancellationToken);
    public Task<PagedResponse<AlertResponse>> GetAlertsAsync(int page, AlertLevel? level, AlertStatus? status, CancellationToken cancellationToken) =>
        GetAsync<PagedResponse<AlertResponse>>($"alerts?page={page}&pageSize=20" +
            (level is null ? "" : $"&level={level.ToString()!.ToLowerInvariant()}") +
            (status is null ? "" : $"&status={status.ToString()!.ToLowerInvariant()}"), cancellationToken);
    public async Task AcknowledgeAsync(Guid alertId, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Post, $"alerts/{alertId:D}/acknowledge", cancellationToken).ConfigureAwait(false);
    }

    private static string PeriodQuery(MetricType metric, DateTimeOffset from, DateTimeOffset until) =>
        $"metric={metric.ToString().ToLowerInvariant()}&from={Uri.EscapeDataString(from.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture))}&to={Uri.EscapeDataString(until.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture))}";

    private async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken) where T : class
    {
        using var response = await SendAsync(HttpMethod.Get, path, cancellationToken).ConfigureAwait(false);
        return await ReadAsync<T>(response, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken) where T : class
    {
        try
        {
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            return await JsonSerializer.DeserializeAsync<T>(stream, ClientJsonContext.Default.Options, cancellationToken).ConfigureAwait(false)
                ?? throw new ClientApiException("Сервер повернув порожню відповідь.");
        }
        catch (JsonException) { throw new ClientApiException("Формат відповіді сервера не відповідає контракту."); }
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, CancellationToken cancellationToken,
        LoginRequest? body = null, string? server = null, bool anonymous = false, string? explicitToken = null)
    {
        server ??= ApiAddress.Normalize(settings.ApiAddress);
        using var request = new HttpRequestMessage(method, new Uri(new Uri(server), "api/v1/" + path));
        if (!anonymous)
        {
            var token = explicitToken ?? await session.TokenAsync(server).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(token)) throw new ClientApiException("Сесія завершилася. Увійдіть знову.", HttpStatusCode.Unauthorized);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        if (body is not null)
        {
            request.Content = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(body, ClientJsonContext.Default.LoginRequest));
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }
        HttpResponseMessage response;
        try { response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false); }
        catch (HttpRequestException) { throw new ClientApiException("Не вдалося з’єднатися із сервером. Перевірте адресу API та мережу."); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { throw new ClientApiException("Сервер не відповідає. Спробуйте ще раз."); }
        if (response.IsSuccessStatusCode) return response;
        using (response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized && !anonymous) await session.ClearAsync().ConfigureAwait(false);
            var message = response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => anonymous ? "Неправильна пошта або пароль." : "Сесія завершилася. Увійдіть знову.",
                HttpStatusCode.Forbidden => "Для цієї дії недостатньо прав.",
                HttpStatusCode.NotFound => "Ці дані або можливість поки недоступні на сервері.",
                HttpStatusCode.Conflict => "Стан даних змінився. Оновіть список.",
                HttpStatusCode.BadRequest => "Перевірте введені дані.",
                _ => "Помилка сервера. Спробуйте пізніше.",
            };
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                try
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    var problem = JsonSerializer.Deserialize(content, ClientJsonContext.Default.ClientProblem);
                    if (problem?.Errors is { Count: > 0 } errors) message = string.Join(" ", errors.Values.SelectMany(values => values));
                }
                catch (JsonException) { /* Зберігаємо зрозуміле повідомлення для некоректного ProblemDetails. */ }
            }
            throw new ClientApiException(message, response.StatusCode);
        }
    }
}
