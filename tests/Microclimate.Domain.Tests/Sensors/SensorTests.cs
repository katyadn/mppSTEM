using Microclimate.Domain.Common;
using Microclimate.Domain.Sensors;

namespace Microclimate.Domain.Tests.Sensors;

public class SensorTests
{
    [Fact]
    public void Create_ValidData_IsActive()
    {
        var sensor = TestData.Sensor();

        Assert.Equal(TestData.RoomId, sensor.RoomId);
        Assert.Equal("SN-001", sensor.SerialNumber);
        Assert.True(sensor.IsActive);
        Assert.Null(sensor.LastSeenAt);
    }

    [Theory]
    [InlineData("", "hash")]
    [InlineData("SN", "")]
    public void Create_MissingRequiredText_Throws(string serial, string keyHash)
    {
        Assert.Throws<DomainException>(() => Sensor.Create(TestData.RoomId, serial, null, keyHash, TestData.Now));
    }

    [Fact]
    public void Create_EmptyRoom_Throws()
    {
        Assert.Throws<DomainException>(() => Sensor.Create(Guid.Empty, "SN", null, "hash", TestData.Now));
    }

    [Fact]
    public void EnsureActive_DeactivatedSensor_Throws_AndActivateRestores()
    {
        var sensor = TestData.Sensor();

        sensor.Deactivate();
        Assert.Throws<DomainException>(sensor.EnsureActive);

        sensor.Activate();
        sensor.EnsureActive();
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
}
