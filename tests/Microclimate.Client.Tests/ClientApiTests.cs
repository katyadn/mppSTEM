using System.Net;
using Microclimate.Client.Core.Services;
using Microclimate.Contracts.Common;
using Microclimate.Contracts.Analytics;

namespace Microclimate.Client.Tests;

// Перевірки HTTP-контрактів, безпеки сесії, адрес API й обробки помилок.
public sealed class ClientApiTests
{
    // Надає JSON ProblemDetails із помилкою періоду для тестування читабельних повідомлень.
    private static readonly string[] PeriodErrors = ["Період завеликий"];
    // Перевіряє: вхід є анонімним, профіль запитується з Bearer, а сховище містить лише access-сесію.
    [Fact]
    public async Task LoginUsesAnonymousRequestThenAuthenticatedProfileAndStoresOnlyAccessSession()
    {
        using var f = new ClientFixture();
        await f.Api.LoginAsync("student@example.test", "fixture-password", CancellationToken.None);
        Assert.Null(f.Requests[0].Authorization);
        Assert.Contains("fixture-password", f.Requests[0].Body!, StringComparison.Ordinal);
        Assert.Equal("Bearer fixture-token", f.Requests[1].Authorization);
        Assert.Equal(UserRole.Operator, f.Session.Current!.User.Role);
        Assert.DoesNotContain("fixture-password", f.Store.Value!, StringComparison.Ordinal);
        Assert.DoesNotContain("refreshToken", f.Store.Value!, StringComparison.Ordinal);
    }
    // Перевіряє: неправильні облікові дані не створюють сесію.
    [Fact]
    public async Task WrongCredentialsDoNotCreateSession()
    {
        using var f = new ClientFixture { Reply = _ => new(HttpStatusCode.Unauthorized) };
        var failure = await Assert.ThrowsAsync<ClientApiException>(() => f.Api.LoginAsync("student@example.test", "wrong", CancellationToken.None));
        Assert.Contains("пароль", failure.Message, StringComparison.Ordinal);
        Assert.Null(f.Session.Current); Assert.Null(f.Store.Value);
    }
    // Перевіряє: hTTP 401 очищає збережену сесію.
    [Fact]
    public async Task UnauthorizedResponseClearsStoredSession()
    {
        using var f = new ClientFixture(); await f.SignInAsync();
        f.Reply = _ => new(HttpStatusCode.Unauthorized);
        await Assert.ThrowsAsync<ClientApiException>(() => f.Api.GetRoomsAsync(1, CancellationToken.None));
        Assert.Null(f.Session.Current); Assert.Null(f.Store.Value);
    }
    // Перевіряє: hTTP 403 зберігає чинну сесію.
    [Fact]
    public async Task ForbiddenResponseKeepsSession()
    {
        using var f = new ClientFixture(); await f.SignInAsync();
        f.Reply = _ => new(HttpStatusCode.Forbidden);
        var failure = await Assert.ThrowsAsync<ClientApiException>(() => f.Api.GetRoomsAsync(1, CancellationToken.None));
        Assert.Equal(HttpStatusCode.Forbidden, failure.StatusCode); Assert.NotNull(f.Session.Current);
    }
    // Перевіряє: прострочений токен очищається без надсилання серверу.
    [Fact]
    public async Task ExpiredSessionIsClearedWithoutSendingItsToken()
    {
        using var f = new ClientFixture(); await f.SignInAsync(); f.Clock.Now = f.Clock.Now.AddHours(2);
        await Assert.ThrowsAsync<ClientApiException>(() => f.Api.GetRoomsAsync(1, CancellationToken.None));
        Assert.Empty(f.Requests); Assert.Null(f.Store.Value);
    }
    // Перевіряє: зміна сервера не передає йому токен попереднього сервера.
    [Fact]
    public async Task ChangingServerNeverSendsTokenToNewServer()
    {
        using var f = new ClientFixture(); await f.SignInAsync(); f.Settings.ApiAddress = "https://other.example.test/";
        await Assert.ThrowsAsync<ClientApiException>(() => f.Api.GetRoomsAsync(1, CancellationToken.None));
        Assert.Empty(f.Requests); Assert.Null(f.Session.Current);
    }
    // Перевіряє: запит ряду містить UTC-межі, метрику й денні інтервали для довгого періоду.
    [Fact]
    public async Task SeriesUsesUtcAndMetricAndDayBucketsForLongerPeriods()
    {
        using var f = new ClientFixture(); await f.SignInAsync();
        await f.Api.GetSeriesAsync(ClientFixture.RoomId, MetricType.Co2, f.Clock.Now.AddDays(-7), f.Clock.Now, CancellationToken.None);
        var request = Assert.Single(f.Requests);
        Assert.Contains("metric=co2", request.Uri.Query, StringComparison.Ordinal);
        Assert.Contains("interval=day", request.Uri.Query, StringComparison.Ordinal);
        Assert.Contains("from=", request.Uri.Query, StringComparison.Ordinal);
        Assert.Contains("to=", request.Uri.Query, StringComparison.Ordinal);
        Assert.DoesNotContain("building", request.Uri.Query, StringComparison.Ordinal);
    }
    // Перевіряє: порожня статистика зберігає Count = 0 і відсутні значення.
    [Fact]
    public async Task EmptySummaryArrayPreservesMissingMeasurements()
    {
        using var f = new ClientFixture(); await f.SignInAsync();
        f.Reply = _ => ClientFixture.Json(Array.Empty<MetricSummaryResponse>());
        var summary = await f.Api.GetSummaryAsync(ClientFixture.RoomId, MetricType.Co2, f.Clock.Now.AddDays(-1), f.Clock.Now, CancellationToken.None);
        Assert.Equal(MetricType.Co2, summary.Metric);
        Assert.Equal(0, summary.Count);
        Assert.Null(summary.Avg);
    }
    // Перевіряє: із серверного масиву статистики вибирається запитана метрика.
    [Fact]
    public async Task SummarySelectsRequestedMetricFromBackendArray()
    {
        using var f = new ClientFixture(); await f.SignInAsync();
        f.Reply = _ => ClientFixture.Json(new MetricSummaryResponse[] { new(MetricType.Temperature, 1, 22, 22, 22, null), new(MetricType.Co2, 1, 900, 900, 900, null) });
        var summary = await f.Api.GetSummaryAsync(ClientFixture.RoomId, MetricType.Co2, f.Clock.Now.AddDays(-1), f.Clock.Now, CancellationToken.None);
        Assert.Equal(MetricType.Co2, summary.Metric);
        Assert.Equal(900, summary.Avg);
    }
    // Перевіряє: серверні помилки валідації перетворюються на читабельне повідомлення.
    [Fact]
    public async Task ValidationProblemIsReadable()
    {
        using var f = new ClientFixture(); await f.SignInAsync();
        f.Reply = _ => ClientFixture.Json(new { errors = new { period = PeriodErrors } }, HttpStatusCode.BadRequest);
        var error = await Assert.ThrowsAsync<ClientApiException>(() => f.Api.GetRoomsAsync(1, CancellationToken.None));
        Assert.Equal("Період завеликий", error.Message);
    }
    // Перевіряє: некоректний JSON і мережевий збій стають клієнтськими помилками.
    [Fact]
    public async Task InvalidJsonAndNetworkErrorsBecomeClientErrors()
    {
        using var f = new ClientFixture(); await f.SignInAsync();
        f.Reply = _ => new(HttpStatusCode.OK) { Content = new StringContent("not-json") };
        await Assert.ThrowsAsync<ClientApiException>(() => f.Api.GetRoomsAsync(1, CancellationToken.None));
        f.Reply = _ => throw new HttpRequestException("internal-transport-detail");
        var failure = await Assert.ThrowsAsync<ClientApiException>(() => f.Api.GetRoomsAsync(1, CancellationToken.None));
        Assert.DoesNotContain("internal-transport-detail", failure.Message, StringComparison.Ordinal);
    }
    // Перевіряє: сесія відновлюється, а відмова сховища залишає нову сесію лише в пам’яті.
    [Fact]
    public async Task SessionCanBeRestoredAndUnavailableSecureStorageUsesMemoryOnly()
    {
        using var f = new ClientFixture(); await f.SignInAsync();
        var restored = new ClientSession(f.Store, f.Clock); await restored.RestoreAsync();
        Assert.Equal("fixture-token", await restored.TokenAsync(f.Settings.ApiAddress));
        await f.Session.ClearAsync(); f.Store.Persist = false; await f.SignInAsync();
        Assert.NotNull(f.Session.Current); Assert.False(f.Session.IsPersistent); Assert.Null(f.Store.Value);
    }
    // Перевіряє: небезпечна або некоректна адреса API відхиляється.
    [Theory]
    [InlineData("http://remote.example.com/")]
    [InlineData("https://name:password@example.com/")]
    [InlineData("https://example.com/?token=secret")]
    [InlineData("file:///tmp/api")]
    public void UnsafeApiAddressesAreRejected(string address) => Assert.Throws<ArgumentException>(() => ApiAddress.Normalize(address));
    // Перевіряє: припустима адреса API приводиться до очікуваного кореневого формату.
    [Theory]
    [InlineData("https://api.example.com", "https://api.example.com/")]
    [InlineData("http://localhost:5195", "http://localhost:5195/")]
    [InlineData("http://10.0.2.2:5195/", "http://10.0.2.2:5195/")]
    public void ApiAddressesAreNormalized(string address, string expected) => Assert.Equal(expected, ApiAddress.Normalize(address));
}
