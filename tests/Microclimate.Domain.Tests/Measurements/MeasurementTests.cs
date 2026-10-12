using Microclimate.Domain.Common;
using Microclimate.Domain.Enums;
using Microclimate.Domain.Measurements;

namespace Microclimate.Domain.Tests.Measurements;

public class MeasurementTests
{
    [Fact]
    public void Record_ValidValue_TakesRoomFromSensorAndStoresUtc()
    {
        // Arrange
        var sensor = TestData.Sensor(MetricType.Temperature);
        var measuredAt = TestData.Now.ToOffset(TimeSpan.FromHours(3));

        // Act
        var measurement = Measurement.Record(sensor, MetricType.Temperature, 22.4, measuredAt, TestData.Now);

        // Assert
        Assert.Equal(sensor.Id, measurement.SensorId);
        Assert.Equal(sensor.RoomId, measurement.RoomId);
        Assert.Equal(22.4, measurement.Value);
        Assert.Equal(TimeSpan.Zero, measurement.MeasuredAt.Offset);
        Assert.Equal(TestData.Now, measurement.MeasuredAt);
    }

    [Fact]
    public void Record_SensorMovedLater_KeepsOriginalRoom()
    {
        var sensor = TestData.Sensor(MetricType.Temperature);
        var measurement = Measurement.Record(sensor, MetricType.Temperature, 22, TestData.Now, TestData.Now);

        sensor.MoveTo(Guid.NewGuid());

        Assert.Equal(TestData.RoomId, measurement.RoomId);
    }

    [Theory]
    [InlineData(MetricType.Humidity, 0)]
    [InlineData(MetricType.Humidity, 100)]
    [InlineData(MetricType.Temperature, -40)]
    [InlineData(MetricType.Temperature, 85)]
    public void Record_ValueOnPhysicalBoundary_IsAllowed(MetricType metric, double value)
    {
        var sensor = TestData.Sensor(metric);

        var measurement = Measurement.Record(sensor, metric, value, TestData.Now, TestData.Now);

        Assert.Equal(value, measurement.Value);
    }

    [Theory]
    [InlineData(MetricType.Humidity, -0.1)]
    [InlineData(MetricType.Humidity, 100.1)]
    [InlineData(MetricType.Temperature, 85.1)]
    [InlineData(MetricType.Co2, -1)]
    [InlineData(MetricType.Temperature, double.NaN)]
    [InlineData(MetricType.Temperature, double.PositiveInfinity)]
    public void Record_InvalidValue_Throws(MetricType metric, double value)
    {
        var sensor = TestData.Sensor(metric);

        Assert.Throws<DomainException>(() => Measurement.Record(sensor, metric, value, TestData.Now, TestData.Now));
    }

    [Fact]
    public void Record_WithinClockSkew_IsAllowed()
    {
        var sensor = TestData.Sensor(MetricType.Co2);

        var measurement = Measurement.Record(sensor, MetricType.Co2, 600, TestData.Now + Measurement.AllowedClockSkew, TestData.Now);

        Assert.Equal(600, measurement.Value);
    }

    [Fact]
    public void Record_MeasuredInFutureBeyondSkew_Throws()
    {
        var sensor = TestData.Sensor(MetricType.Co2);
        var measuredAt = TestData.Now + Measurement.AllowedClockSkew + TimeSpan.FromSeconds(1);

        Assert.Throws<DomainException>(() => Measurement.Record(sensor, MetricType.Co2, 600, measuredAt, TestData.Now));
    }

    [Fact]
    public void Record_UnsupportedMetric_Throws()
    {
        var sensor = TestData.Sensor(MetricType.Temperature);

        Assert.Throws<DomainException>(() => Measurement.Record(sensor, MetricType.Co2, 600, TestData.Now, TestData.Now));
    }

    [Fact]
    public void Record_UndefinedMetric_Throws()
    {
        var sensor = TestData.Sensor(MetricType.Temperature);

        Assert.Throws<DomainException>(() => Measurement.Record(sensor, (MetricType)99, 1, TestData.Now, TestData.Now));
    }

    [Fact]
    public void Record_NullSensor_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Measurement.Record(null!, MetricType.Co2, 600, TestData.Now, TestData.Now));
    }
}
