using Microclimate.Domain.Buildings;
using Microclimate.Domain.Common;
using Microclimate.Domain.Enums;
using Microclimate.Domain.Rooms;

namespace Microclimate.Domain.Tests.Buildings;

public class BuildingAndRoomTests
{
    [Fact]
    public void Building_Create_TrimsTextAndStoresUtcTime()
    {
        var building = Building.Create("  Корпус 1  ", "  ", TestData.Now.ToOffset(TimeSpan.FromHours(3)));

        Assert.NotEqual(Guid.Empty, building.Id);
        Assert.Equal("Корпус 1", building.Name);
        Assert.Null(building.Address);
        Assert.Equal(TimeSpan.Zero, building.CreatedAt.Offset);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Building_EmptyName_Throws(string? name)
    {
        Assert.Throws<DomainException>(() => Building.Create(name!, null, TestData.Now));
    }

    [Fact]
    public void Building_NameAtMaxLength_IsAllowed_AndLongerThrows()
    {
        Building.Create(new string('a', Building.NameMaxLength), null, TestData.Now);

        Assert.Throws<DomainException>(() => Building.Create(new string('a', Building.NameMaxLength + 1), null, TestData.Now));
    }

    [Fact]
    public void Building_Update_ChangesNameAndAddress()
    {
        var building = Building.Create("A", null, TestData.Now);

        building.Update("B", "вул. Володимирська, 60");

        Assert.Equal("B", building.Name);
        Assert.Equal("вул. Володимирська, 60", building.Address);
    }

    [Fact]
    public void Room_Create_ValidData_StoresValues()
    {
        var buildingId = Guid.NewGuid();

        var room = Room.Create(buildingId, "101", 1, 24.5m, RoomType.Classroom, TestData.Now);

        Assert.Equal(buildingId, room.BuildingId);
        Assert.Equal("101", room.Name);
        Assert.Equal(1, room.Floor);
        Assert.Equal(24.5m, room.AreaM2);
        Assert.Equal(RoomType.Classroom, room.Type);
    }

    [Theory]
    [InlineData(Room.MinFloor)]
    [InlineData(Room.MaxFloor)]
    public void Room_FloorOnBoundary_IsAllowed(int floor)
    {
        var room = Room.Create(Guid.NewGuid(), "101", floor, null, RoomType.Office, TestData.Now);

        Assert.Equal(floor, room.Floor);
    }

    [Theory]
    [InlineData(Room.MinFloor - 1)]
    [InlineData(Room.MaxFloor + 1)]
    public void Room_FloorOutOfRange_Throws(int floor)
    {
        Assert.Throws<DomainException>(() => Room.Create(Guid.NewGuid(), "101", floor, null, RoomType.Office, TestData.Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Room_NonPositiveArea_Throws(int area)
    {
        Assert.Throws<DomainException>(() => Room.Create(Guid.NewGuid(), "101", 1, area, RoomType.Office, TestData.Now));
    }

    [Fact]
    public void Room_EmptyBuilding_Throws()
    {
        Assert.Throws<DomainException>(() => Room.Create(Guid.Empty, "101", 1, null, RoomType.Office, TestData.Now));
    }

    [Fact]
    public void Room_UndefinedType_Throws()
    {
        Assert.Throws<DomainException>(() => Room.Create(Guid.NewGuid(), "101", 1, null, (RoomType)42, TestData.Now));
    }
}
