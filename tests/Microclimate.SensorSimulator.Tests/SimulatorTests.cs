using System.Net;

using Microclimate.Contracts.Common;
using Microclimate.Contracts.Measurements;

using Microsoft.Extensions.Logging.Abstractions;

namespace Microclimate.SensorSimulator.Tests;

public class SimulatorTests
{
    private static readonly DateTimeOffset Afternoon = new(2026, 10, 20, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Next_SameSeed_ReturnsSameRoundedValuesWithinNoise()
    {
        var first = new SensorSignalGenerator(SimulationScenario.Normal, seed: 7, new OverheatOptions()).Next(Afternoon);
        var second = new SensorSignalGenerator(SimulationScenario.Normal, seed: 7, new OverheatOptions()).Next(Afternoon);

        Assert.Equal(first, second);
        Assert.Equal(MetricSignal.Defaults.Count, first.Count);
        foreach (var (item, signal) in first.Zip(MetricSignal.Defaults))
        {
            Assert.Equal(Afternoon, item.MeasuredAt);
            Assert.Equal(Math.Round(item.Value, signal.Decimals), item.Value);
            Assert.InRange(item.Value, signal.ValueAt(15) - signal.Noise - 0.5, signal.ValueAt(15) + signal.Noise + 0.5);
        }

        // О 15:00 пік температури: 22 + 1.5.
        Assert.Equal(23.5, MetricSignal.Defaults[0].ValueAt(15), precision: 6);
    }

    [Fact]
    public void Next_Overheat_RisesToPeak_HoldsAndReturnsToNormal()
    {
        MetricSignal[] flat = [new(MetricType.Temperature, Base: 28.5, Amplitude: 0, PeakHour: 0, Noise: 0, Decimals: 1)];
        var options = new OverheatOptions { PeakTemperature = 29, StepPerMeasurement = 0.1, HoldMeasurements = 3 };
        var generator = new SensorSignalGenerator(SimulationScenario.Overheat, seed: 1, options, flat);

        var values = Enumerable.Range(0, 10).Select(_ => generator.Next(Afternoon)[0].Value).ToList();

        Assert.Equal([28.6, 28.7, 28.8, 28.9, 29.0, 29.0, 29.0, 29.0, 28.5, 28.5], values);
    }

    [Fact]
    public void Validate_MissingSettings_ReportsEachProblem()
    {
        var options = new SimulatorOptions { ApiBaseUrl = null, Period = TimeSpan.Zero };
        options.Sensors.Add(new SimulatedSensorOptions());

        var result = new SimulatorOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
        Assert.Equal(4, result.Failures!.Count());
    }

    [Theory]
    [InlineData(HttpStatusCode.Accepted, SendResult.Accepted)]
    [InlineData(HttpStatusCode.BadRequest, SendResult.Rejected)]
    [InlineData(HttpStatusCode.Unauthorized, SendResult.Unauthorized)]
    public async Task SendAsync_MapsStatusCode_AndSendsDeviceKey(HttpStatusCode status, SendResult expected)
    {
        using var handler = new StubHandler(_ => new HttpResponseMessage(status));
        var client = new MeasurementApiClient(new StubFactory(handler), NullLogger<MeasurementApiClient>.Instance);

        var result = await client.SendAsync(Batch(), "key-1", CancellationToken.None);

        Assert.Equal(expected, result);
        Assert.Equal("key-1", handler.LastRequest!.Headers.GetValues(MeasurementApiClient.DeviceKeyHeader).Single());
        Assert.EndsWith(MeasurementApiClient.MeasurementsPath, handler.LastRequest.RequestUri!.AbsolutePath, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendAsync_ApiDown_ReturnsUnavailableWithoutThrowing()
    {
        using var handler = new StubHandler(_ => throw new HttpRequestException("connection refused"));
        var client = new MeasurementApiClient(new StubFactory(handler), NullLogger<MeasurementApiClient>.Instance);

        Assert.Equal(SendResult.Unavailable, await client.SendAsync(Batch(), "key-1", CancellationToken.None));
    }

    [Fact]
    public async Task RunSensor_Unauthorized_StopsAfterFirstBatch()
    {
        var sender = new RecordingSender(SendResult.Unauthorized);
        var sensor = new SimulatedSensorOptions { SensorId = Guid.NewGuid(), DeviceKey = "wrong" };
        var options = Microsoft.Extensions.Options.Options.Create(new SimulatorOptions());
        using var worker = new SimulatorWorker(options, sender, TimeProvider.System, NullLogger<SimulatorWorker>.Instance);

        await worker.RunSensorAsync(sensor, new SensorSignalGenerator(SimulationScenario.Normal, 1, new OverheatOptions()), TimeSpan.FromMilliseconds(10), CancellationToken.None);

        var batch = Assert.Single(sender.Batches);
        Assert.Equal(sensor.SensorId, batch.SensorId);
    }

    private static MeasurementBatchRequest Batch() =>
        new(Guid.NewGuid(), [new MeasurementItem(MetricType.Temperature, 22, Afternoon)]);

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(respond(request));
        }
    }

    private sealed class StubFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(handler, disposeHandler: false) { BaseAddress = new Uri("http://localhost:5195/") };
    }

    private sealed class RecordingSender(SendResult result) : IMeasurementSender
    {
        public List<MeasurementBatchRequest> Batches { get; } = [];

        public Task<SendResult> SendAsync(MeasurementBatchRequest batch, string deviceKey, CancellationToken cancellationToken)
        {
            Batches.Add(batch);
            return Task.FromResult(result);
        }
    }
}
