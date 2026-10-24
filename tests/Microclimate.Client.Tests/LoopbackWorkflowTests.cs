using System.Net;
using System.Net.Sockets;
using System.Text;
using Microclimate.Client.Core.Services;
using Microclimate.Client.Core.ViewModels;

namespace Microclimate.Client.Tests;

internal sealed class LoopbackServer : IAsyncDisposable
{
    private readonly HttpListener listener = new();
    private readonly Task loop;
    public string Address { get; }
    public List<SeenRequest> Requests { get; } = [];
    public LoopbackServer(Func<SeenRequest, HttpResponseMessage> respond)
    {
        var portProbe = new TcpListener(IPAddress.Loopback, 0);
        portProbe.Start();
        var port = ((IPEndPoint)portProbe.LocalEndpoint).Port;
        portProbe.Stop();
        Address = $"http://127.0.0.1:{port}/";
        listener.Prefixes.Add(Address); listener.Start();
        loop = Task.Run(async () =>
        {
            while (listener.IsListening)
            {
                HttpListenerContext context;
                try { context = await listener.GetContextAsync(); }
                catch (HttpListenerException) { break; }
                catch (ObjectDisposedException) { break; }
                using var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
                var request = new SeenRequest(context.Request.HttpMethod, context.Request.Url!, context.Request.Headers["Authorization"], await reader.ReadToEndAsync());
                Requests.Add(request);
                using var response = respond(request);
                context.Response.StatusCode = (int)response.StatusCode;
                if (response.Headers.Location is { } redirect) context.Response.RedirectLocation = redirect.AbsoluteUri;
                if (response.Content is not null)
                {
                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    context.Response.ContentType = "application/json";
                    context.Response.ContentLength64 = bytes.Length;
                    await context.Response.OutputStream.WriteAsync(bytes);
                }
                context.Response.Close();
            }
        });
    }
    public async ValueTask DisposeAsync()
    {
        listener.Stop(); listener.Close();
        await loop.WaitAsync(TimeSpan.FromSeconds(5));
    }
}

public sealed class LoopbackWorkflowTests
{
    [Fact]
    public async Task ClientCompletesMainWorkflowsThroughRealLocalHttp()
    {
        using var fixture = new ClientFixture();
        await using var server = new LoopbackServer(fixture.Respond);
        fixture.Settings.ApiAddress = server.Address;
        using var http = ClientTransport.Create();
        var api = new ClientApi(http, fixture.Settings, fixture.Session, fixture.Clock);
        var login = new LoginViewModel(api, fixture.Navigation) { Email = "student@example.test", Password = "fixture-password" };
        await login.LoginCommand.ExecuteAsync(null);
        Assert.Equal("dashboard", fixture.Navigation.Destination);
        var dashboard = new DashboardViewModel(api, fixture.Navigation); await dashboard.InitializeAsync();
        Assert.Single(dashboard.Rooms);
        var details = new RoomDetailsViewModel(api, fixture.Navigation, fixture.Clock);
        details.SelectRoom(ClientFixture.RoomId, "101"); await details.InitializeAsync();
        Assert.Equal(2, details.Series.Count); Assert.Equal("82/100 · Прийнятно", details.ComfortText);
        var alerts = new AlertsViewModel(api, fixture.Session, fixture.Navigation); await alerts.InitializeAsync();
        await alerts.AcknowledgeCommand.ExecuteAsync(Assert.Single(alerts.Alerts));
        Assert.True(fixture.Acknowledged);
        Assert.All(server.Requests.Where(request => !request.Uri.AbsolutePath.EndsWith("auth/login", StringComparison.Ordinal)), request => Assert.Equal("Bearer fixture-token", request.Authorization));
    }

    [Fact]
    public async Task RedirectDoesNotForwardLoginCredentials()
    {
        await using var server = new LoopbackServer(_ => new HttpResponseMessage(HttpStatusCode.TemporaryRedirect)
        { Headers = { Location = new Uri("https://different.example.test/") } });
        using var fixture = new ClientFixture(); fixture.Settings.ApiAddress = server.Address;
        using var http = ClientTransport.Create();
        var api = new ClientApi(http, fixture.Settings, fixture.Session, fixture.Clock);
        await Assert.ThrowsAsync<ClientApiException>(() => api.LoginAsync("student@example.test", "fixture-password", CancellationToken.None));
        Assert.Single(server.Requests); Assert.Null(fixture.Session.Current);
    }
}
