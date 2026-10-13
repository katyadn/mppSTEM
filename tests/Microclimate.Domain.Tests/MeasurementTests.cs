using Microclimate.Domain.Common;
using Microclimate.Domain.Enums;
using Microclimate.Domain.Measurements;

namespace Microclimate.Domain.Tests;

public class MeasurementTests
{
    [Fact]
    public void Record_ValidValue_StoresSensorAndUtcTime()
    {
        var sensor = TestData.Sensor();

        var measurement = Measurement.Record(sensor, MetricType.Temperature, 22.4, TestData.Now.ToOffset(TimeSpan.FromHours(3)), TestData.Now);

        Assert.Equal(sensor.Id, measurement.SensorId);
        Assert.Equal(22.4, measurement.Value);
        Assert.Equal(TimeSpan.Zero, measurement.MeasuredAt.Offset);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Record_HumidityOnPhysicalBoundary_IsAllowed(double value)
    {
        var measurement = Measurement.Record(TestData.Sensor(), MetricType.Humidity, value, TestData.Now, TestData.Now);

        Assert.Equal(value, measurement.Value);
    }

    [Theory]
    [InlineData(MetricType.Humidity, -0.1)]
    [InlineData(MetricType.Humidity, 100.1)]
    [InlineData(MetricType.Temperature, 85.1)]
    [InlineData(MetricType.Temperature, double.NaN)]
    public void Record_InvalidValue_Throws(MetricType metric, double value)
    {
        Assert.Throws<DomainException>(() => Measurement.Record(TestData.Sensor(), metric, value, TestData.Now, TestData.Now));
    }

    [Fact]
    public void Record_MeasuredInFutureBeyondSkew_Throws()
    {
        var measuredAt = TestData.Now + Measurement.AllowedClockSkew + TimeSpan.FromSeconds(1);

        Assert.Throws<DomainException>(() => Measurement.Record(TestData.Sensor(), MetricType.Co2, 600, measuredAt, TestData.Now));
    }

    [Fact]
    public void Record_DeactivatedSensor_Throws()
    {
        var sensor = TestData.Sensor();
        sensor.Deactivate();

        Assert.Throws<DomainException>(() => Measurement.Record(sensor, MetricType.Co2, 600, TestData.Now, TestData.Now));
    }
}
