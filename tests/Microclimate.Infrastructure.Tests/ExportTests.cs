using System.Text;
using System.Text.Json;

using Microclimate.Application.Abstractions.Files;
using Microclimate.Domain.Enums;
using Microclimate.Domain.Measurements;
using Microclimate.Domain.Sensors;
using Microclimate.Infrastructure.Files;

namespace Microclimate.Infrastructure.Tests;

public class ExportTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ExportAsync_Csv_WritesHeaderAndInvariantRows_AndLeavesStreamOpen()
    {
        var (sensor, items) = Sample();
        using var stream = new MemoryStream();

        await new MeasurementExporter().ExportAsync(ToAsync(items), stream, ExportFormat.Csv, CancellationToken.None);

        var lines = Encoding.UTF8.GetString(stream.ToArray()).TrimEnd().Split(Environment.NewLine);
        Assert.Equal(
        [
            MeasurementExporter.CsvHeader,
            $"2026-10-01T11:00:00.0000000+00:00,{sensor.Id},Temperature,21.5",
            $"2026-10-01T11:30:00.0000000+00:00,{sensor.Id},Co2,900",
        ],
            lines);
        Assert.True(stream.CanWrite);
    }

    [Fact]
    public async Task ExportAsync_Json_WritesArrayOfMeasurements()
    {
        var (sensor, items) = Sample();
        using var stream = new MemoryStream();

        await new MeasurementExporter().ExportAsync(ToAsync(items), stream, ExportFormat.Json, CancellationToken.None);

        using var json = JsonDocument.Parse(stream.ToArray());
        var rows = json.RootElement.EnumerateArray().ToList();
        Assert.Equal(2, rows.Count);
        Assert.Equal(sensor.Id, rows[0].GetProperty("sensorId").GetGuid());
        Assert.Equal("Temperature", rows[0].GetProperty("metric").GetString());
        Assert.Equal(21.5, rows[0].GetProperty("value").GetDouble());
        Assert.Equal(Now.AddHours(-1), rows[0].GetProperty("measuredAt").GetDateTimeOffset());
    }

    private static (Sensor Sensor, Measurement[] Items) Sample()
    {
        var sensor = Sensor.Create(Guid.NewGuid(), "SN-1", null, "hash", Now);
        return (sensor,
        [
            Measurement.Record(sensor, MetricType.Temperature, 21.5, Now.AddHours(-1), Now),
            Measurement.Record(sensor, MetricType.Co2, 900, Now.AddMinutes(-30), Now),
        ]);
    }

    private static async IAsyncEnumerable<Measurement> ToAsync(IEnumerable<Measurement> items)
    {
        foreach (var item in items)
        {
            await Task.Yield();
            yield return item;
        }
    }
}
