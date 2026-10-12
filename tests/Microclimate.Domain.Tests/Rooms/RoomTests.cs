using Microclimate.Domain.Common;
using Microclimate.Domain.Enums;
using Microclimate.Domain.Rooms;

namespace Microclimate.Domain.Tests.Rooms;

public class RoomTests
{
    [Fact]
    public void Create_ValidData_TrimsNameAndStoresUtcTime()
    {
        var room = Room.Create("  101  ", 1, 24.5m, RoomType.Classroom, TestData.Now.ToOffset(TimeSpan.FromHours(3)));

        Assert.NotEqual(Guid.Empty, room.Id);
        Assert.Equal("101", room.Name);
        Assert.Equal(1, room.Floor);
        Assert.Equal(24.5m, room.AreaM2);
        Assert.Equal(RoomType.Classroom, room.Type);
        Assert.Equal(TimeSpan.Zero, room.CreatedAt.Offset);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptyName_Throws(string? name)
    {
        Assert.Throws<DomainException>(() => Room.Create(name!, 1, null, RoomType.Office, TestData.Now));
    }

    [Fact]
    public void Create_NameAtMaxLength_IsAllowed_AndLongerThrows()
    {
        Room.Create(new string('a', Room.NameMaxLength), 1, null, RoomType.Office, TestData.Now);

        Assert.Throws<DomainException>(() =>
            Room.Create(new string('a', Room.NameMaxLength + 1), 1, null, RoomType.Office, TestData.Now));
    }

    [Theory]
    [InlineData(Room.MinFloor)]
    [InlineData(Room.MaxFloor)]
    public void Create_FloorOnBoundary_IsAllowed(int floor)
    {
        var room = Room.Create("101", floor, null, RoomType.Office, TestData.Now);

        Assert.Equal(floor, room.Floor);
    }

    [Theory]
    [InlineData(Room.MinFloor - 1)]
    [InlineData(Room.MaxFloor + 1)]
    public void Create_FloorOutOfRange_Throws(int floor)
    {
        Assert.Throws<DomainException>(() => Room.Create("101", floor, null, RoomType.Office, TestData.Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_NonPositiveArea_Throws(int area)
    {
        Assert.Throws<DomainException>(() => Room.Create("101", 1, area, RoomType.Office, TestData.Now));
    }

    [Fact]
    public void Create_UndefinedType_Throws()
    {
        Assert.Throws<DomainException>(() => Room.Create("101", 1, null, (RoomType)42, TestData.Now));
    }

    [Fact]
    public void Update_ValidData_ChangesFields()
    {
        var room = Room.Create("101", 1, null, RoomType.Office, TestData.Now);

        room.Update("Серверна", -1, 12m, RoomType.ServerRoom);

        Assert.Equal("Серверна", room.Name);
        Assert.Equal(-1, room.Floor);
        Assert.Equal(12m, room.AreaM2);
        Assert.Equal(RoomType.ServerRoom, room.Type);
    }
}
