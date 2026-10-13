using Microclimate.Domain.Common;
using Microclimate.Domain.Enums;
using Microclimate.Domain.Rooms;
using Microclimate.Domain.Sensors;
using Microclimate.Domain.Users;
using Microclimate.Domain.ValueObjects;

namespace Microclimate.Domain.Tests;

public class EntityTests
{
    [Fact]
    public void Room_Create_TrimsName()
    {
        var room = Room.Create("  101  ", 1, 24.5m, RoomType.Classroom, TestData.Now);

        Assert.Equal("101", room.Name);
        Assert.Equal(RoomType.Classroom, room.Type);
    }

    [Theory]
    [InlineData(Room.MinFloor - 1, null)]
    [InlineData(1, 0)]
    public void Room_InvalidFloorOrArea_Throws(int floor, int? area)
    {
        Assert.Throws<DomainException>(() => Room.Create("101", floor, area, RoomType.Office, TestData.Now));
    }

    [Fact]
    public void Sensor_MarkSeen_DoesNotMoveBack()
    {
        var sensor = TestData.Sensor();

        sensor.MarkSeen(TestData.Now);
        sensor.MarkSeen(TestData.Now.AddMinutes(-5));

        Assert.Equal(TestData.Now, sensor.LastSeenAt);
    }

    [Fact]
    public void Sensor_EmptySerialNumber_Throws()
    {
        Assert.Throws<DomainException>(() => Sensor.Create(TestData.RoomId, " ", null, "hash", TestData.Now));
    }

    [Fact]
    public void User_Create_NormalizesEmail()
    {
        var user = User.Create("  Operator@Example.COM ", "Оператор", "hash", UserRole.Operator, TestData.Now);

        Assert.Equal("operator@example.com", user.Email);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("Ім'я <user@example.com>")]
    public void User_InvalidEmail_Throws(string email)
    {
        Assert.Throws<DomainException>(() => User.Create(email, "User", "hash", UserRole.Viewer, TestData.Now));
    }

    [Fact]
    public void DateRange_Contains_IsHalfOpen()
    {
        var range = DateRange.Create(TestData.Now, TestData.Now.AddHours(1));

        Assert.True(range.Contains(TestData.Now));
        Assert.False(range.Contains(TestData.Now.AddHours(1)));
    }

    [Fact]
    public void DateRange_InvalidOrTooLong_Throws()
    {
        Assert.Throws<DomainException>(() => DateRange.Create(TestData.Now, TestData.Now));
        Assert.Throws<DomainException>(() => DateRange.Create(TestData.Now, TestData.Now.AddDays(91), TimeSpan.FromDays(90)));
    }
}
