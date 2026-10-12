using Microclimate.Domain.Common;
using Microclimate.Domain.Enums;
using Microclimate.Domain.Measurements;

namespace Microclimate.Domain.Tests.Measurements;

public class MeasurementTests
{
    [Fact]
    public void Record_ValidValue_StoresSensorAndUtcTime()
    {
        var sensor = TestData.Sensor();
        var measuredAt = TestData.Now.ToOffset(TimeSpan.FromHours(3));

        var measurement = Measurement.Record(sensor, MetricType.Temperature, 22.4, measuredAt, TestData.Now);

        Assert.Equal(sensor.Id, measurement.SensorId);
        Assert.Equal(MetricType.Temperature, measurement.Metric);
        Assert.Equal(22.4, measurement.Value);
        Assert.Equal(TimeSpan.Zero, measurement.MeasuredAt.Offset);
        Assert.Equal(TestData.Now, measurement.MeasuredAt);
    }

    [Theory]
    [InlineData(MetricType.Humidity, 0)]
    [InlineData(MetricType.Humidity, 100)]
    [InlineData(MetricType.Temperature, -40)]
    [InlineData(MetricType.Temperature, 85)]
    public void Record_ValueOnPhysicalBoundary_IsAllowed(MetricType metric, double value)
    {
        var measurement = Measurement.Record(TestData.Sensor(), metric, value, TestData.Now, TestData.Now);

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
        Assert.Throws<DomainException>(() => Measurement.Record(TestData.Sensor(), metric, value, TestData.Now, TestData.Now));
    }

    [Fact]
    public void Record_WithinClockSkew_IsAllowed()
    {
        var measurement = Measurement.Record(
            TestData.Sensor(), MetricType.Co2, 600, TestData.Now + Measurement.AllowedClockSkew, TestData.Now);

        Assert.Equal(600, measurement.Value);
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

    [Fact]
    public void Record_UndefinedMetric_Throws()
    {
        Assert.Throws<DomainException>(() => Measurement.Record(TestData.Sensor(), (MetricType)99, 1, TestData.Now, TestData.Now));
    }

    [Fact]
    public void Record_NullSensor_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Measurement.Record(null!, MetricType.Co2, 600, TestData.Now, TestData.Now));
    }
}
