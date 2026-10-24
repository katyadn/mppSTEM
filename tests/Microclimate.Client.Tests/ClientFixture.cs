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

internal sealed class MemorySettings : IClientSettings { public string ApiAddress { get; set; } = "https://api.example.test/"; }
internal sealed class MemoryStore : ISessionStore
{
    public string? Value { get; private set; }
    public bool Persist { get; set; } = true;
    public Task<string?> ReadAsync() => Task.FromResult(Value);
    public Task<bool> WriteAsync(string value) { Value = Persist ? value : null; return Task.FromResult(Persist); }
    public Task ClearAsync() { Value = null; return Task.CompletedTask; }
}
internal sealed class TestClock : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => Now;
}
internal sealed class TestNavigation : INavigationService
{
    public string Destination { get; private set; } = "";
    public Guid? RoomId { get; private set; }
    public Task GoToDashboardAsync() { Destination = "dashboard"; return Task.CompletedTask; }
    public Task GoToLoginAsync() { Destination = "login"; return Task.CompletedTask; }
    public Task GoToSettingsAsync() { Destination = "settings"; return Task.CompletedTask; }
    public Task GoBackAsync() { Destination = "back"; return Task.CompletedTask; }
    public Task GoToRoomDetailsAsync(Guid? roomId = null, string? roomName = null) { Destination = "room"; RoomId = roomId; return Task.CompletedTask; }
}
internal sealed record SeenRequest(string Method, Uri Uri, string? Authorization, string? Body);
internal sealed class FixtureHandler : HttpMessageHandler
{
    public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> ResponseFactory { get; set; } = null!;
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => ResponseFactory(request, cancellationToken);
}
internal sealed class ClientFixture : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = CreateOptions();
    private readonly FixtureHandler handler = new();
    private readonly HttpClient http;
    public static readonly Guid RoomId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid AlertId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public MemoryStore Store { get; } = new();
    public MemorySettings Settings { get; } = new();
    public TestClock Clock { get; } = new();
    public TestNavigation Navigation { get; } = new();
    public ClientSession Session { get; }
    public ClientApi Api { get; }
    public List<SeenRequest> Requests { get; } = [];
    public Func<SeenRequest, HttpResponseMessage>? Reply { get; set; }
    public UserRole Role { get; set; } = UserRole.Operator;
    public bool Acknowledged { get; private set; }
    public bool DelayRooms { get; set; }
    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
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
    public async Task SignInAsync()
    {
        await Api.LoginAsync("student@example.test", "fixture-password", CancellationToken.None);
        Requests.Clear();
    }
    public static HttpResponseMessage Json(object value, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(JsonSerializer.Serialize(value, JsonOptions), Encoding.UTF8, "application/json") };
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
    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
    public void Dispose() => http.Dispose();
}
