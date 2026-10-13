using Microclimate.Domain.Alerts;
using Microclimate.Domain.Common;
using Microclimate.Domain.Enums;

namespace Microclimate.Domain.Tests;

public class AlertTests
{
    private static Alert NewAlert() =>
        Alert.Raise(TestData.RoomId, null, MetricType.Co2, AlertLevel.Warning, 1300, 1200, "CO₂ вище норми", TestData.Now);

    [Fact]
    public void Acknowledge_OpenAlert_SetsUserAndStatus()
    {
        var alert = NewAlert();

        alert.Acknowledge(TestData.UserId, TestData.Now.AddMinutes(2));

        Assert.Equal(AlertStatus.Acknowledged, alert.Status);
        Assert.Equal(TestData.UserId, alert.AcknowledgedByUserId);
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
    public void Acknowledge_ResolvedAlert_Throws()
    {
        var alert = NewAlert();
        alert.Resolve(TestData.Now);

        Assert.Throws<DomainException>(() => alert.Acknowledge(TestData.UserId, TestData.Now));
        Assert.Throws<DomainException>(() => alert.Resolve(TestData.Now));
    }

    [Fact]
    public void Acknowledge_BeforeRaised_Throws()
    {
        Assert.Throws<DomainException>(() => NewAlert().Acknowledge(TestData.UserId, TestData.Now.AddSeconds(-1)));
    }
}
