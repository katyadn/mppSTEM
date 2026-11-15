using System.Net;
using System.Net.Sockets;
using System.Text;

using Microclimate.Contracts.Common;
using Microclimate.Contracts.Measurements;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Microclimate.SensorSimulator.Tests;

public class SimulatorTests
{
    private static readonly DateTimeOffset Afternoon = new(2026, 10, 20,
        15, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(307)]
    [InlineData(308)]
    public async Task SendAsync_Redirect_DoesNotContactOtherOrigin(
        int status)
    {
        using var source = new TcpListener(IPAddress.Loopback, 0);
        using var target = new TcpListener(IPAddress.Loopback, 0);
        source.Start();
        target.Start();
        var sourcePort = ((IPEndPoint)source.LocalEndpoint).Port;
        var targetPort = ((IPEndPoint)target.LocalEndpoint).Port;
        using var timeout = new CancellationTokenSource(
            TimeSpan.FromSeconds(10));
        var sourceRequest = ReplyOnceAsync(source,
            $"HTTP/1.1 {status} Redirect\r\n"
            + $"Location: http://127.0.0.1:{targetPort}/received\r\n",
            timeout.Token);
        var targetRequest = ReplyOnceAsync(target,
            "HTTP/1.1 202 Accepted\r\n", timeout.Token);

        var services = new ServiceCollection();
        services.Configure<SimulatorOptions>(options =>
            options.ApiBaseUrl = new Uri(
                $"http://127.0.0.1:{sourcePort}/"));
        services.AddMeasurementApiClient();
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var client = new MeasurementApiClient(factory,
            NullLogger<MeasurementApiClient>.Instance);

        SendResult result;
        try
        {
            result = await client.SendAsync(Batch(),
                "test-key-not-a-secret", timeout.Token);
            Assert.True(await sourceRequest);
        }
        finally
        {
            await timeout.CancelAsync();
            await Task.WhenAll(sourceRequest, targetRequest);
        }

        Assert.Equal(SendResult.Rejected, result);
        Assert.False(await targetRequest);
    }

    private static async Task<bool> ReplyOnceAsync(
        TcpListener listener, string response, CancellationToken token)
    {
        try
        {
            using var socket = await listener.AcceptTcpClientAsync(token);
            await using var stream = socket.GetStream();
            using var reader = new StreamReader(stream, leaveOpen: true);
            while (await reader.ReadLineAsync(token) is { Length: > 0 })
            {
            }

            var bytes = Encoding.ASCII.GetBytes(response
                + "Content-Length: 0\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(bytes, token);
            return true;
        }
        catch (OperationCanceledException) when (
            token.IsCancellationRequested)
        {
            return false;
        }
    }

    [Fact]
    public void Next_SameSeed_ReturnsSameRoundedValuesWithinNoise()
    {
        var first = new SensorSignalGenerator(SimulationScenario.Normal,
            seed: 7, new OverheatOptions()).Next(Afternoon);
        var second = new SensorSignalGenerator(SimulationScenario.Normal,
            seed: 7, new OverheatOptions()).Next(Afternoon);

        Assert.Equal(first, second);
        Assert.Equal(MetricSignal.Defaults.Count, first.Count);
        foreach (var (item, signal) in first.Zip(MetricSignal.Defaults))
        {
            Assert.Equal(Afternoon, item.MeasuredAt);
            Assert.Equal(Math.Round(item.Value, signal.Decimals),
                item.Value);
            Assert.InRange(item.Value,
                signal.ValueAt(15) - signal.Noise - 0.5,
                signal.ValueAt(15) + signal.Noise + 0.5);
        }

        // О 15:00 пік температури: 22 + 1.5.
        Assert.Equal(23.5, MetricSignal.Defaults[0].ValueAt(15),
            precision: 6);
    }

    [Fact]
    public void Next_Overheat_RisesToPeak_HoldsAndReturnsToNormal()
    {
        MetricSignal[] flat = [new(MetricType.Temperature, Base: 28.5,
            Amplitude: 0, PeakHour: 0, Noise: 0, Decimals: 1)];
        var options = new OverheatOptions { PeakTemperature = 29,
            StepPerMeasurement = 0.1, HoldMeasurements = 3 };
        var generator = new SensorSignalGenerator(
            SimulationScenario.Overheat, seed: 1, options, flat);

        var values = Enumerable.Range(0,
            10).Select(_ => generator.Next(Afternoon)[0].Value).ToList();

        Assert.Equal([28.6, 28.7, 28.8, 28.9, 29.0, 29.0, 29.0, 29.0, 28.5,
            28.5], values);
    }

    [Fact]
    public void Validate_MissingSettings_ReportsEachProblem()
    {
        var options = new SimulatorOptions { ApiBaseUrl = null,
            Period = TimeSpan.Zero };
        options.Sensors.Add(new SimulatedSensorOptions());

        var result = new SimulatorOptionsValidator().Validate(null,
            options);

        Assert.True(result.Failed);
        Assert.Equal(4, result.Failures!.Count());
    }

    [Theory]
    [InlineData(HttpStatusCode.Accepted, SendResult.Accepted)]
    [InlineData(HttpStatusCode.BadRequest, SendResult.Rejected)]
    [InlineData(HttpStatusCode.Unauthorized, SendResult.Unauthorized)]
    public async Task SendAsync_MapsStatusCode_AndSendsDeviceKey(
        HttpStatusCode status, SendResult expected)
    {
        using var handler = new StubHandler(_ => new HttpResponseMessage(
            status));
        var client = new MeasurementApiClient(new StubFactory(handler),
            NullLogger<MeasurementApiClient>.Instance);

        var result = await client.SendAsync(Batch(), "key-1",
            CancellationToken.None);

        Assert.Equal(expected, result);
        Assert.Equal("key-1", handler.LastRequest!.Headers.GetValues(
            MeasurementApiClient.DeviceKeyHeader).Single());
        Assert.EndsWith(MeasurementApiClient.MeasurementsPath,
            handler.LastRequest.RequestUri!.AbsolutePath,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendAsync_ApiDown_ReturnsUnavailableWithoutThrowing()
    {
        using var handler = new StubHandler(
            _ => throw new HttpRequestException("connection refused"));
        var client = new MeasurementApiClient(new StubFactory(handler),
            NullLogger<MeasurementApiClient>.Instance);

        Assert.Equal(SendResult.Unavailable,
            await client.SendAsync(Batch(), "key-1",
            CancellationToken.None));
    }

    [Fact]
    public async Task
        Worker_UnauthorizedSensorStops_OthersKeepSendingUntilShutdown()
    {
        var good = new SimulatedSensorOptions { SensorId = Guid.NewGuid(),
            DeviceKey = "good" };
        var wrong = new SimulatedSensorOptions { SensorId = Guid.NewGuid(),
            DeviceKey = "wrong" };
        var settings = new SimulatorOptions { Period =
            TimeSpan.FromMilliseconds(10) };
        settings.Sensors.Add(good);
        settings.Sensors.Add(wrong);
        var sender = new RecordingSender(
            key => key == "good" ? SendResult.Accepted :
            SendResult.Unauthorized, waitForGoodBatches: 3);
        using var worker = new SimulatorWorker(
            Microsoft.Extensions.Options.Options.Create(settings), sender,
                TimeProvider.System, NullLogger<SimulatorWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        await sender.GoodBatchesSent.WaitAsync(TimeSpan.FromSeconds(10));
        await worker.StopAsync(CancellationToken.None);

        Assert.Single(sender.Batches, b => b.SensorId == wrong.SensorId);
        Assert.True(sender.Batches.Count(
            b => b.SensorId == good.SensorId) >= 3);
    }

    private static MeasurementBatchRequest Batch() =>
        new(Guid.NewGuid(), [new MeasurementItem(MetricType.Temperature,
            22, Afternoon)]);

    private sealed class StubHandler(Func<HttpRequestMessage,
        HttpResponseMessage> respond) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(respond(request));
        }
    }

    private sealed class StubFactory(
        HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(handler, disposeHandler: false) { BaseAddress = new Uri(
                "http://localhost:5195/") };
    }

    private sealed class RecordingSender(Func<string, SendResult> respond,
        int waitForGoodBatches) : IMeasurementSender
    {
        private readonly TaskCompletionSource _goodBatchesSent = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private int _goodCount;

        public System.Collections.Concurrent
            .ConcurrentQueue<MeasurementBatchRequest> Batches { get; } =
            new();

        public Task GoodBatchesSent => _goodBatchesSent.Task;

        public Task<SendResult> SendAsync(MeasurementBatchRequest batch,
            string deviceKey, CancellationToken cancellationToken)
        {
            Batches.Enqueue(batch);
            var result = respond(deviceKey);
            if (result == SendResult.Accepted && Interlocked.Increment(
                ref _goodCount) >= waitForGoodBatches)
            {
                _goodBatchesSent.TrySetResult();
            }

            return Task.FromResult(result);
        }
    }
}
