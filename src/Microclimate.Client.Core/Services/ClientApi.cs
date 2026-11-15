using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microclimate.Contracts.Alerts;
using Microclimate.Contracts.Analytics;
using Microclimate.Contracts.Auth;
using Microclimate.Contracts.Common;
using Microclimate.Contracts.Rooms;
using Microclimate.Contracts.Thresholds;

namespace Microclimate.Client.Core.Services;

// Помилка клієнтського запиту з повідомленням для користувача та необов’язковим HTTP-статусом.
public sealed class ClientApiException(string message, HttpStatusCode? statusCode = null) : Exception(message)
{
    // HTTP-статус відповіді; null для локальної помилки або недоступності мережі.
    public HttpStatusCode? StatusCode { get; } = statusCode;
}

// Контракт клієнтських HTTP-сценаріїв: вхід, кімнати, аналітика та сповіщення.
public interface IClientApi
{
    // Виконує вхід і завантажує профіль; у сесії зберігається лише access-токен із терміном дії.
    Task LoginAsync(string email, string password, CancellationToken cancellationToken);

    // Перевіряє придатність збереженої сесії та оновлює профіль через auth/me.
    Task<bool> RestoreSessionAsync(CancellationToken cancellationToken);

    // Отримує сторінку кімнат; відсутні в серверній відповіді виміри позначає як NoData.
    Task<PagedResponse<RoomSummaryResponse>> GetRoomsAsync(int page, CancellationToken cancellationToken);

    Task<RoomResponse> GetRoomAsync(Guid roomId, CancellationToken cancellationToken);
    Task<IReadOnlyList<LatestMetricResponse>> GetLatestAsync(Guid roomId, CancellationToken cancellationToken);

    // Обирає статистику запитаної метрики з серверного масиву; за її відсутності повертає Count = 0 і null-значення.
    Task<MetricSummaryResponse> GetSummaryAsync(
        Guid roomId,
        MetricType metric,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken
    );

    // Отримує часовий ряд із погодинними інтервалами до однієї доби включно та денними для довших періодів.
    Task<IReadOnlyList<SeriesPointResponse>> GetSeriesAsync(
        Guid roomId,
        MetricType metric,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken
    );

    Task<ComfortIndexResponse> GetComfortAsync(Guid roomId, CancellationToken cancellationToken);

    Task<PagedResponse<AlertResponse>> GetAlertsAsync(
        int page,
        AlertLevel? level,
        AlertStatus? status,
        CancellationToken cancellationToken
    );

    // Надсилає підтвердження сповіщення; остаточну перевірку прав виконує сервер.
    Task AcknowledgeAsync(Guid alertId, CancellationToken cancellationToken);

    // Читає ефективні пороги (власні або типові) та зберігає межі вибраної метрики.
    Task<IReadOnlyList<ThresholdResponse>> GetThresholdsAsync(Guid roomId, CancellationToken cancellationToken);
    Task<ThresholdResponse> UpdateThresholdAsync(
        Guid roomId,
        MetricType metric,
        UpdateThresholdRequest request,
        CancellationToken cancellationToken
    );
}

// Виконує запити до api/v1, прив’язує access-сесію до сервера та перетворює помилки HTTP на зрозумілі повідомлення.
public sealed class ClientApi(HttpClient http, IClientSettings settings, ClientSession session, TimeProvider clock)
    : IClientApi
{
    public async Task LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        var server = ApiAddress.Normalize(settings.ApiAddress);
        await session.ClearAsync().ConfigureAwait(false);
        using var response = await SendAsync(
                HttpMethod.Post,
                "auth/login",
                cancellationToken,
                new LoginRequest(email, password),
                server: server,
                anonymous: true
            )
            .ConfigureAwait(false);
        var tokens = await ReadAsync<TokenResponse>(response, cancellationToken).ConfigureAwait(false);
        if (
            string.IsNullOrWhiteSpace(tokens.AccessToken)
            || tokens.ExpiresIn is <= 0 or > 86_400
            || !string.Equals(tokens.TokenType, "Bearer", StringComparison.OrdinalIgnoreCase)
        )
            throw new ClientApiException("Сервер повернув некоректну сесію.");
        using var me = await SendAsync(
                HttpMethod.Get,
                "auth/me",
                cancellationToken,
                server: server,
                explicitToken: tokens.AccessToken
            )
            .ConfigureAwait(false);
        var user = await ReadAsync<UserResponse>(me, cancellationToken).ConfigureAwait(false);
        if (server != ApiAddress.Normalize(settings.ApiAddress))
            throw new ClientApiException("Адреса API змінилася. Увійдіть знову.");
        await session
            .SaveAsync(
                new StoredSession(server, tokens.AccessToken, clock.GetUtcNow().AddSeconds(tokens.ExpiresIn), user)
            )
            .ConfigureAwait(false);
    }

    public async Task<bool> RestoreSessionAsync(CancellationToken cancellationToken)
    {
        var server = ApiAddress.Normalize(settings.ApiAddress);
        if (await session.TokenAsync(server).ConfigureAwait(false) is null)
            return false;
        using var response = await SendAsync(HttpMethod.Get, "auth/me", cancellationToken).ConfigureAwait(false);
        var user = await ReadAsync<UserResponse>(response, cancellationToken).ConfigureAwait(false);
        if (session.Current is { } current)
            await session.SaveAsync(current with { User = user }).ConfigureAwait(false);
        return true;
    }

    public async Task<PagedResponse<RoomSummaryResponse>> GetRoomsAsync(int page, CancellationToken cancellationToken)
    {
        var rooms = await GetAsync<PagedResponse<RoomResponse>>($"rooms?page={page}&pageSize=20", cancellationToken)
            .ConfigureAwait(false);
        // Дані доповнюються чинними endpoints; не обчислюємо доменний комфорт на клієнті.
        // Чотири кімнати одночасно обмежують навантаження при сторінці з 20 кімнат.
        using var slots = new SemaphoreSlim(4);
        var items = await Task.WhenAll(
                rooms.Items.Select(async room =>
                {
                    await slots.WaitAsync(cancellationToken).ConfigureAwait(false);
                    try
                    {
                        var latest = await GetAsync<List<LatestMetricResponse>>(
                                $"rooms/{room.Id:D}/measurements/latest",
                                cancellationToken
                            )
                            .ConfigureAwait(false);
                        var comfort = await GetComfortAsync(room.Id, cancellationToken).ConfigureAwait(false);
                        return new RoomSummaryResponse(
                            room.Id,
                            Guid.Empty,
                            "",
                            room.Name,
                            room.Floor,
                            room.Type,
                            comfort.Category.ToString(),
                            latest
                        );
                    }
                    finally
                    {
                        slots.Release();
                    }
                })
            )
            .ConfigureAwait(false);
        return new(items, rooms.Page, rooms.PageSize, rooms.TotalCount);
    }

    public Task<RoomResponse> GetRoomAsync(Guid roomId, CancellationToken cancellationToken) =>
        GetAsync<RoomResponse>($"rooms/{roomId:D}", cancellationToken);

    public async Task<IReadOnlyList<LatestMetricResponse>> GetLatestAsync(
        Guid roomId,
        CancellationToken cancellationToken
    ) =>
        await GetAsync<List<LatestMetricResponse>>($"rooms/{roomId:D}/measurements/latest", cancellationToken)
            .ConfigureAwait(false);

    public async Task<MetricSummaryResponse> GetSummaryAsync(
        Guid roomId,
        MetricType metric,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken
    )
    {
        var summaries = await GetAsync<List<MetricSummaryResponse>>(
                $"rooms/{roomId:D}/analytics/summary?{PeriodQuery(metric, from, until)}",
                cancellationToken
            )
            .ConfigureAwait(false);
        return summaries.FirstOrDefault(summary => summary.Metric == metric)
            ?? new MetricSummaryResponse(metric, 0, null, null, null, null);
    }

    public async Task<IReadOnlyList<SeriesPointResponse>> GetSeriesAsync(
        Guid roomId,
        MetricType metric,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken
    ) =>
        await GetAsync<List<SeriesPointResponse>>(
                $"rooms/{roomId:D}/analytics/series?{PeriodQuery(metric, from, until)}&interval={((until - from).TotalDays <= 1 ? "hour" : "day")}",
                cancellationToken
            )
            .ConfigureAwait(false);

    public Task<ComfortIndexResponse> GetComfortAsync(Guid roomId, CancellationToken cancellationToken) =>
        GetAsync<ComfortIndexResponse>($"rooms/{roomId:D}/analytics/comfort", cancellationToken);

    public Task<PagedResponse<AlertResponse>> GetAlertsAsync(
        int page,
        AlertLevel? level,
        AlertStatus? status,
        CancellationToken cancellationToken
    ) =>
        GetAsync<PagedResponse<AlertResponse>>(
            $"alerts?page={page}&pageSize=20"
                + (level is null ? "" : $"&level={level.ToString()!.ToLowerInvariant()}")
                + (status is null ? "" : $"&status={status.ToString()!.ToLowerInvariant()}"),
            cancellationToken
        );

    public async Task AcknowledgeAsync(Guid alertId, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Post, $"alerts/{alertId:D}/acknowledge", cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ThresholdResponse>> GetThresholdsAsync(
        Guid roomId,
        CancellationToken cancellationToken
    ) =>
        await GetAsync<List<ThresholdResponse>>($"rooms/{roomId:D}/thresholds", cancellationToken)
            .ConfigureAwait(false);

    public async Task<ThresholdResponse> UpdateThresholdAsync(
        Guid roomId,
        MetricType metric,
        UpdateThresholdRequest request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        using var response = await SendAsync(
                HttpMethod.Put,
                $"rooms/{roomId:D}/thresholds/{metric.ToString().ToLowerInvariant()}",
                cancellationToken,
                body: request
            )
            .ConfigureAwait(false);
        return await ReadAsync<ThresholdResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    // Формує query-параметри метрики та меж періоду в UTC ISO 8601 з URI-кодуванням.
    private static string PeriodQuery(MetricType metric, DateTimeOffset from, DateTimeOffset until) =>
        $"metric={metric.ToString().ToLowerInvariant()}&from={Uri.EscapeDataString(from.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture))}&to={Uri.EscapeDataString(until.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture))}";

    // Виконує авторизований GET і десеріалізує тіло, звільняючи HTTP-відповідь.
    private async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
        where T : class
    {
        using var response = await SendAsync(HttpMethod.Get, path, cancellationToken).ConfigureAwait(false);
        return await ReadAsync<T>(response, cancellationToken).ConfigureAwait(false);
    }

    // Читає JSON через згенеровані метадані; порожнє або некоректне тіло перетворює на ClientApiException.
    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            return await JsonSerializer
                .DeserializeAsync<T>(stream, ClientJsonContext.Default.Options, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new ClientApiException("Сервер повернув порожню відповідь.");
        }
        catch (JsonException)
        {
            throw new ClientApiException("Формат відповіді сервера не відповідає контракту.");
        }
    }

    // Додає Bearer-токен для потрібного сервера; HTTP 401 очищає сесію, 403 зберігає її, помилки мережі та валідації локалізуються.
    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        CancellationToken cancellationToken,
        object? body = null,
        string? server = null,
        bool anonymous = false,
        string? explicitToken = null
    )
    {
        server ??= ApiAddress.Normalize(settings.ApiAddress);
        using var request = new HttpRequestMessage(method, new Uri(new Uri(server), "api/v1/" + path));
        if (!anonymous)
        {
            var token = explicitToken ?? await session.TokenAsync(server).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(token))
                throw new ClientApiException("Сесія завершилася. Увійдіть знову.", HttpStatusCode.Unauthorized);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        if (body is not null)
        {
            request.Content = new ByteArrayContent(
                JsonSerializer.SerializeToUtf8Bytes(body, body.GetType(), ClientJsonContext.Default)
            );
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }
        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            throw new ClientApiException("Не вдалося з’єднатися із сервером. Перевірте адресу API та мережу.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ClientApiException("Сервер не відповідає. Спробуйте ще раз.");
        }
        if (response.IsSuccessStatusCode)
            return response;
        using (response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized && !anonymous)
                await session.ClearAsync().ConfigureAwait(false);
            var message = response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => anonymous
                    ? "Неправильна пошта або пароль."
                    : "Сесія завершилася. Увійдіть знову.",
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
                    if (problem?.Errors is { Count: > 0 } errors)
                        message = string.Join(" ", errors.Values.SelectMany(values => values));
                }
                catch (JsonException)
                { /* Зберігаємо зрозуміле повідомлення для некоректного ProblemDetails. */
                }
            }
            throw new ClientApiException(message, response.StatusCode);
        }
    }
}
