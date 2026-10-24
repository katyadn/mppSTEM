using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microclimate.Client.Core.Services;
using Microclimate.Contracts.Alerts;
using Microclimate.Contracts.Analytics;
using Microclimate.Contracts.Auth;
using Microclimate.Contracts.Common;
using Microclimate.Contracts.Rooms;

namespace Microclimate.Client.Tests;

// Тестові налаштування API в пам’яті без звернень до Preferences.
internal sealed class MemorySettings : IClientSettings { 
    // Коренева адреса сервера; ClientApi додає до неї api/v1/.
    public string ApiAddress { get; set; } = "https://api.example.test/"; }
// Тестове сховище, яке може імітувати недоступність постійного збереження сесії.
internal sealed class MemoryStore : ISessionStore
{
    // Серіалізована тестова сесія в пам’яті.
    public string? Value { get; private set; }
    // Чи імітувати успішне постійне збереження.
    public bool Persist { get; set; } = true;
    // Повертає поточний серіалізований запис тестового сховища.
    public Task<string?> ReadAsync() => Task.FromResult(Value);
    // Зберігає запис лише при Persist і повертає результат спроби.
    public Task<bool> WriteAsync(string value) { Value = Persist ? value : null; return Task.FromResult(Persist); }
    // Очищає серіалізований запис тестового сховища.
    public Task ClearAsync() { Value = null; return Task.CompletedTask; }
}
// Керований UTC-годинник для відтворюваних перевірок терміну дії сесії й періодів.
internal sealed class TestClock : TimeProvider
{
    // Керований поточний момент UTC для тесту.
    public DateTimeOffset Now { get; set; } = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);
    // Повертає заданий тестом момент замість системного часу.
    public override DateTimeOffset GetUtcNow() => Now;
}
// Записує останній перехід і кімнату без запуску MAUI Shell.
internal sealed class TestNavigation : INavigationService
{
    // Назва останнього запитаного екрана.
    public string Destination { get; private set; } = "";
    // Ідентифікатор кімнати для запитів аналітики або переходів.
    public Guid? RoomId { get; private set; }
    // Відкриває головний екран зі списком кімнат.
    public Task GoToDashboardAsync() { Destination = "dashboard"; return Task.CompletedTask; }
    // Відкриває екран входу.
    public Task GoToLoginAsync() { Destination = "login"; return Task.CompletedTask; }
    // Відкриває налаштування клієнта.
    public Task GoToSettingsAsync() { Destination = "settings"; return Task.CompletedTask; }
    // Повертається на попередній екран.
    public Task GoBackAsync() { Destination = "back"; return Task.CompletedTask; }
    // Відкриває деталі кімнати з її ідентифікатором і необов’язковою назвою.
    public Task GoToRoomDetailsAsync(Guid? roomId = null, string? roomName = null) { Destination = "room"; RoomId = roomId; return Task.CompletedTask; }
}
// Знімок HTTP-запиту для перевірки маршруту, авторизації та JSON у тестах.
internal sealed record SeenRequest(string Method, Uri Uri, string? Authorization, string? Body);
// Передає HTTP-запити тестовому обробнику замість мережевого транспорту.
internal sealed class FixtureHandler : HttpMessageHandler
{
    // Тестова функція формування відповіді для отриманого запиту.
    public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> ResponseFactory { get; set; } = null!;
    // Передає запит і скасування налаштованій функції відповіді.
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => ResponseFactory(request, cancellationToken);
}
// Збирає справжній ClientApi з керованим HTTP-обробником, годинником, сесією та навігацією.
internal sealed class ClientFixture : IDisposable
{
    // Спільні правила JSON для тестових відповідей, узгоджені з серверними enum.
    private static readonly JsonSerializerOptions JsonOptions = CreateOptions();
    // Контрольований HTTP-обробник для запису й підміни відповідей.
    private readonly FixtureHandler handler = new();
    // HttpClient, життєвим циклом якого керує fixture.
    private readonly HttpClient http;
    // Сталий ідентифікатор кімнати у відповідях fixture.
    public static readonly Guid RoomId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    // Сталий ідентифікатор сповіщення у відповідях fixture.
    public static readonly Guid AlertId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    // Сховище сесії з керованим результатом збереження.
    public MemoryStore Store { get; } = new();
    // Тестові налаштування адреси сервера.
    public MemorySettings Settings { get; } = new();
    // Керований годинник для запитів і сесії.
    public TestClock Clock { get; } = new();
    // Записувач переходів ViewModel.
    public TestNavigation Navigation { get; } = new();
    // Справжній менеджер сесії над тестовим сховищем.
    public ClientSession Session { get; }
    // Справжній ClientApi над контрольованим транспортом.
    public ClientApi Api { get; }
    // Знімки отриманих запитів для перевірки HTTP-поведінки.
    public List<SeenRequest> Requests { get; } = [];
    // Необов’язкова підміна відповіді для окремого запиту.
    public Func<SeenRequest, HttpResponseMessage>? Reply { get; set; }
    // Роль, яку повертає тестовий auth/me.
    public UserRole Role { get; set; } = UserRole.Operator;
    // Чи отримав fixture запит підтвердження сповіщення.
    public bool Acknowledged { get; private set; }
    // Чи затримати rooms-запит до скасування для перевірки життєвого циклу.
    public bool DelayRooms { get; set; }
    // Сигнал початку затриманого запиту для синхронізації тесту.
    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    // Створює ізольовані сервіси й транспорт із записом запитів та керованими відповідями.
    public ClientFixture()
    {
        Session = new ClientSession(Store, Clock);
        handler.ResponseFactory = async (request, token) =>
        {
            var seen = new SeenRequest(request.Method.Method, request.RequestUri!, request.Headers.Authorization?.ToString(), request.Content is null ? null : await request.Content.ReadAsStringAsync(token));
            Requests.Add(seen);
            if (DelayRooms && seen.Uri.AbsolutePath.EndsWith("/rooms", StringComparison.Ordinal))
            { Started.TrySetResult(); await Task.Delay(Timeout.InfiniteTimeSpan, token); }
            return Reply?.Invoke(seen) ?? Respond(seen);
        };
        http = new HttpClient(handler);
        Api = new ClientApi(http, Settings, Session, Clock);
    }
    // Створює тестову сесію через ClientApi й очищає журнал запитів підготовчого входу.
    public async Task SignInAsync()
    {
        await Api.LoginAsync("student@example.test", "fixture-password", CancellationToken.None);
        Requests.Clear();
    }
    // Серіалізує об’єкт у JSON-відповідь із заданим HTTP-статусом.
    public static HttpResponseMessage Json(object value, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(JsonSerializer.Serialize(value, JsonOptions), Encoding.UTF8, "application/json") };
    // Повертає контрактні тестові відповіді для входу, кімнат, аналітики та сповіщень.
    public HttpResponseMessage Respond(SeenRequest request)
    {
        var path = request.Uri.AbsolutePath;
        if (path.EndsWith("auth/login", StringComparison.Ordinal)) return Json(new TokenResponse("fixture-token", "", 3600));
        if (path.EndsWith("auth/me", StringComparison.Ordinal)) return Json(new UserResponse(Guid.NewGuid(), "student@example.test", Role, Clock.Now));
        if (path.EndsWith("/rooms", StringComparison.Ordinal))
        {
            var second = request.Uri.Query.Contains("page=2", StringComparison.Ordinal);
            return Json(new PagedResponse<RoomResponse>([new(second ? AlertId : RoomId, Guid.Empty, second ? "102" : "101", 1, 25, "Office", Clock.Now)], second ? 2 : 1, 20, 2));
        }
        if (path.EndsWith("/summary", StringComparison.Ordinal)) return Json(new MetricSummaryResponse[] { new(request.Uri.Query.Contains("metric=co2", StringComparison.Ordinal) ? MetricType.Co2 : MetricType.Temperature, 2, 20, 24, 22, null) });
        if (path.EndsWith("/series", StringComparison.Ordinal)) return Json(new SeriesPointResponse[] { new(Clock.Now.AddHours(-2), 1, 20, 20, 20), new(Clock.Now.AddHours(-1), 0, null, null, null), new(Clock.Now, 1, 24, 24, 24) });
        if (path.EndsWith("/comfort", StringComparison.Ordinal)) return Json(new ComfortIndexResponse(82, ComfortCategory.Acceptable, []));
        if (path.EndsWith("/acknowledge", StringComparison.Ordinal)) { Acknowledged = true; return new(HttpStatusCode.NoContent); }
        if (path.EndsWith("/alerts", StringComparison.Ordinal)) return Json(new PagedResponse<AlertResponse>([new(AlertId, RoomId, null, MetricType.Co2, AlertLevel.Critical, Acknowledged ? AlertStatus.Acknowledged : AlertStatus.Open, 1900, 1750, "Високий CO₂", Clock.Now, null, null, null)], 1, 20, 1));
        if (path.Contains("/rooms/", StringComparison.Ordinal)) return Json(new RoomResponse(RoomId, Guid.Empty, "101", 1, 25, "Office", Clock.Now));
        return new(HttpStatusCode.NotFound);
    }
    // Налаштовує web JSON і рядкові enum у camelCase для fixture.
    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
    // Звільняє HttpClient разом із тестовим HTTP-обробником.
    public void Dispose() => http.Dispose();
}
