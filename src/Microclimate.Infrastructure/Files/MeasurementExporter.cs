using System.Globalization;
using System.Text;
using System.Text.Json;

using Microclimate.Application.Abstractions.Files;
using Microclimate.Domain.Measurements;

namespace Microclimate.Infrastructure.Files;

/// <summary>
/// Потоковий експорт вимірів: кожен вимір записується одразу після читання з БД,
/// тому весь набір не тримається в пам'яті. Потік <c>destination</c> не закривається.
/// </summary>
public sealed class MeasurementExporter : IMeasurementExporter
{
    public const string CsvHeader = "MeasuredAt,SensorId,Metric,Value";

    // Utf8JsonWriter накопичує байти в буфері; після цього обсягу вони асинхронно скидаються в потік.
    private const int JsonFlushThresholdBytes = 16 * 1024;

    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    public Task ExportAsync(IAsyncEnumerable<Measurement> measurements, Stream destination, ExportFormat format, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(measurements);
        ArgumentNullException.ThrowIfNull(destination);

        return format switch
        {
            ExportFormat.Csv => WriteCsvAsync(measurements, destination, cancellationToken),
            ExportFormat.Json => WriteJsonAsync(measurements, destination, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Невідомий формат експорту."),
        };
    }

    private static async Task WriteCsvAsync(IAsyncEnumerable<Measurement> measurements, Stream destination, CancellationToken cancellationToken)
    {
        await using var writer = new StreamWriter(destination, Utf8WithoutBom, leaveOpen: true);
        await writer.WriteLineAsync(CsvHeader.AsMemory(), cancellationToken);

        await foreach (var m in measurements.WithCancellation(cancellationToken))
        {
            var line = string.Create(CultureInfo.InvariantCulture, $"{m.MeasuredAt:O},{m.SensorId},{m.Metric},{m.Value}");
            await writer.WriteLineAsync(line.AsMemory(), cancellationToken);
        }

        await writer.FlushAsync(cancellationToken);
    }

    private static async Task WriteJsonAsync(IAsyncEnumerable<Measurement> measurements, Stream destination, CancellationToken cancellationToken)
    {
        await using var writer = new Utf8JsonWriter(destination);
        writer.WriteStartArray();

        await foreach (var m in measurements.WithCancellation(cancellationToken))
        {
            writer.WriteStartObject();
            writer.WriteString("measuredAt", m.MeasuredAt);
            writer.WriteString("sensorId", m.SensorId);
            writer.WriteString("metric", m.Metric.ToString());
            writer.WriteNumber("value", m.Value);
            writer.WriteEndObject();

            if (writer.BytesPending > JsonFlushThresholdBytes)
            {
                await writer.FlushAsync(cancellationToken);
            }
        }

        writer.WriteEndArray();
        await writer.FlushAsync(cancellationToken);
    }
}
