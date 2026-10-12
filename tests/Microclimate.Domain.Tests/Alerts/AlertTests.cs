using Microclimate.Domain.Alerts;
using Microclimate.Domain.Common;
using Microclimate.Domain.Enums;

namespace Microclimate.Domain.Tests.Alerts;

public class AlertTests
{
    private static Alert NewAlert() =>
        Alert.Raise(TestData.RoomId, null, MetricType.Co2, AlertLevel.Warning, 1300, 1200, "CO₂ вище норми", TestData.Now);

    [Fact]
    public void Raise_ValidData_IsOpen()
    {
        var alert = NewAlert();

        Assert.Equal(AlertStatus.Open, alert.Status);
        Assert.Equal(1300, alert.Value);
        Assert.Equal(1200, alert.ThresholdValue);
        Assert.Equal(TestData.Now, alert.RaisedAt);
        Assert.Null(alert.AcknowledgedAt);
        Assert.Null(alert.ResolvedAt);
    }

    [Fact]
    public void Acknowledge_OpenAlert_SetsUserAndTime()
    {
        var alert = NewAlert();

        alert.Acknowledge(TestData.UserId, TestData.Now.AddMinutes(2));

        Assert.Equal(AlertStatus.Acknowledged, alert.Status);
        Assert.Equal(TestData.UserId, alert.AcknowledgedByUserId);
        Assert.Equal(TestData.Now.AddMinutes(2), alert.AcknowledgedAt);
    }

    [Fact]
    public void Acknowledge_AlreadyAcknowledged_Throws()
    {
        var alert = NewAlert();
        alert.Acknowledge(TestData.UserId, TestData.Now);

        Assert.Throws<DomainException>(() => alert.Acknowledge(TestData.UserId, TestData.Now));
    }

    [Fact]
    public void Acknowledge_ResolvedAlert_Throws()
    {
        var alert = NewAlert();
        alert.Resolve(TestData.Now);

        Assert.Throws<DomainException>(() => alert.Acknowledge(TestData.UserId, TestData.Now));
    }

    [Fact]
    public void Acknowledge_EmptyUser_Throws()
    {
        Assert.Throws<DomainException>(() => NewAlert().Acknowledge(Guid.Empty, TestData.Now));
    }

    [Fact]
    public void Acknowledge_BeforeRaised_Throws()
    {
        Assert.Throws<DomainException>(() => NewAlert().Acknowledge(TestData.UserId, TestData.Now.AddSeconds(-1)));
    }

    [Fact]
    public void Resolve_OpenAlert_IsResolved()
    {
        var alert = NewAlert();

        alert.Resolve(TestData.Now.AddMinutes(10));

        Assert.Equal(AlertStatus.Resolved, alert.Status);
        Assert.Equal(TestData.Now.AddMinutes(10), alert.ResolvedAt);
    }

    [Fact]
    public void Resolve_AcknowledgedAlert_IsResolved()
    {
        var alert = NewAlert();
        alert.Acknowledge(TestData.UserId, TestData.Now);

        alert.Resolve(TestData.Now.AddMinutes(1));

        Assert.Equal(AlertStatus.Resolved, alert.Status);
    }

    [Fact]
    public void Resolve_AlreadyResolved_Throws()
    {
        var alert = NewAlert();
        alert.Resolve(TestData.Now);

        Assert.Throws<DomainException>(() => alert.Resolve(TestData.Now));
    }

    [Fact]
    public void Raise_EmptySensorId_Throws()
    {
        Assert.Throws<DomainException>(() =>
            Alert.Raise(TestData.RoomId, Guid.Empty, MetricType.Co2, AlertLevel.Warning, 1300, 1200, "m", TestData.Now));
    }

    [Fact]
    public void Raise_TooLongMessage_Throws()
    {
        var message = new string('x', Alert.MessageMaxLength + 1);

        Assert.Throws<DomainException>(() =>
            Alert.Raise(TestData.RoomId, null, MetricType.Co2, AlertLevel.Warning, 1300, 1200, message, TestData.Now));
    }

    [Fact]
    public void Raise_UndefinedLevel_Throws()
    {
        Assert.Throws<DomainException>(() =>
            Alert.Raise(TestData.RoomId, null, MetricType.Co2, (AlertLevel)7, 1300, 1200, "m", TestData.Now));
    }
}
