using Microclimate.Domain.Common;
using Microclimate.Domain.Enums;
using Microclimate.Domain.Sensors;

namespace Microclimate.Domain.Tests.Sensors;

public class SensorTests
{
    [Fact]
    public void Create_DuplicateCapabilities_StoresDistinctMetrics()
    {
        var sensor = TestData.Sensor(MetricType.Co2, MetricType.Co2, MetricType.Temperature);

        Assert.Equal(2, sensor.Capabilities.Count);
        Assert.True(sensor.IsActive);
        Assert.True(sensor.Supports(MetricType.Co2));
        Assert.False(sensor.Supports(MetricType.Noise));
    }

    [Fact]
    public void Create_NoCapabilities_Throws()
    {
        Assert.Throws<DomainException>(() =>
            Sensor.Create(TestData.RoomId, "SN", null, "hash", [], TestData.Now));
    }

    [Fact]
    public void Create_UndefinedCapability_Throws()
    {
        Assert.Throws<DomainException>(() =>
            Sensor.Create(TestData.RoomId, "SN", null, "hash", [(MetricType)99], TestData.Now));
    }

    [Fact]
    public void Create_NullCapabilities_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Sensor.Create(TestData.RoomId, "SN", null, "hash", null!, TestData.Now));
    }

    [Theory]
    [InlineData("", "hash")]
    [InlineData("SN", "")]
    public void Create_MissingRequiredText_Throws(string serial, string keyHash)
    {
        Assert.Throws<DomainException>(() =>
            Sensor.Create(TestData.RoomId, serial, null, keyHash, [MetricType.Co2], TestData.Now));
    }

    [Fact]
    public void EnsureCanReport_UnsupportedMetric_Throws()
    {
        var sensor = TestData.Sensor(MetricType.Temperature);

        Assert.Throws<DomainException>(() => sensor.EnsureCanReport(MetricType.Co2));
    }

    [Fact]
    public void EnsureCanReport_DeactivatedSensor_Throws_AndActivateRestores()
    {
        var sensor = TestData.Sensor(MetricType.Temperature);

        sensor.Deactivate();
        Assert.Throws<DomainException>(() => sensor.EnsureCanReport(MetricType.Temperature));

        sensor.Activate();
        sensor.EnsureCanReport(MetricType.Temperature);
    }

    [Fact]
    public void MarkSeen_OlderMoment_DoesNotMoveBack()
    {
        var sensor = TestData.Sensor();

        sensor.MarkSeen(TestData.Now);
        sensor.MarkSeen(TestData.Now.AddMinutes(-5));

        Assert.Equal(TestData.Now, sensor.LastSeenAt);

        sensor.MarkSeen(TestData.Now.AddMinutes(1));
        Assert.Equal(TestData.Now.AddMinutes(1), sensor.LastSeenAt);
    }

    [Fact]
    public void MoveTo_AnotherRoom_ChangesRoom_EmptyThrows()
    {
        var sensor = TestData.Sensor();
        var newRoom = Guid.NewGuid();

        sensor.MoveTo(newRoom);

        Assert.Equal(newRoom, sensor.RoomId);
        Assert.Throws<DomainException>(() => sensor.MoveTo(Guid.Empty));
    }

    [Fact]
    public void RotateKey_NewHash_ReplacesHash()
    {
        var sensor = TestData.Sensor();

        sensor.RotateKey("new-hash");

        Assert.Equal("new-hash", sensor.DeviceKeyHash);
        Assert.Throws<DomainException>(() => sensor.RotateKey(" "));
    }
}
