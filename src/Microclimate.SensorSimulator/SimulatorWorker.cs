using Microclimate.Contracts.Measurements;

using Microsoft.Extensions.Options;

namespace Microclimate.SensorSimulator;

/// <summary>
/// Кожен датчик працює в окремому асинхронному циклі з PeriodicTimer; усі цикли чекають через Task.WhenAll.
/// Зупинка — через CancellationToken від BackgroundService.
/// </summary>
public sealed partial class SimulatorWorker(
    IOptions<SimulatorOptions> options,
    IMeasurementSender sender,
    TimeProvider time,
    ILogger<SimulatorWorker> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        LogStarted(settings.Sensors.Count, settings.Period);

        return Task.WhenAll(settings.Sensors.Select((sensor, index) =>
            RunSensorAsync(
                sensor,
                new SensorSignalGenerator(sensor.Scenario, settings.Seed + index, settings.Overheat),
                settings.Period,
                stoppingToken)));
    }

    /// <summary>Перший пакет надсилається одразу, далі — раз на період.</summary>
    internal async Task RunSensorAsync(SimulatedSensorOptions sensor, SensorSignalGenerator generator, TimeSpan period, CancellationToken cancellationToken)
    {
        // Наявність id перевірено SimulatorOptionsValidator під час старту.
        var sensorId = sensor.SensorId.GetValueOrDefault();
        using var timer = new PeriodicTimer(period, time);
        try
        {
            do
            {
                var batch = new MeasurementBatchRequest(sensorId, generator.Next(time.GetLocalNow()));
                var result = await sender.SendAsync(batch, sensor.DeviceKey, cancellationToken);
                if (result == SendResult.Unauthorized)
                {
                    LogUnauthorized(sensorId);
                    return;
                }
            }
            while (await timer.WaitForNextTickAsync(cancellationToken));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            LogStopped(sensorId);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Симулятор запущено: датчиків {Count}, період {Period}")]
    private partial void LogStarted(int count, TimeSpan period);

    [LoggerMessage(Level = LogLevel.Error, Message = "Датчик {SensorId}: неправильний X-Device-Key (401), датчик зупинено.")]
    private partial void LogUnauthorized(Guid sensorId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Датчик {SensorId} зупинено.")]
    private partial void LogStopped(Guid sensorId);
}
