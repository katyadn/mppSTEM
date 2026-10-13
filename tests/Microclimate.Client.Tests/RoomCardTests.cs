using Microclimate.Client.Core.Presentation;
using Microclimate.Contracts.Common;
using Microclimate.Contracts.Rooms;

namespace Microclimate.Client.Tests;

public sealed class RoomCardTests
{
    [Fact]
    public void RoomCardIgnoresLegacyBuildingFields()
    {
        var id = Guid.NewGuid();
        var response = new RoomSummaryResponse(id, Guid.Empty, "Legacy building", "101", 1, "Classroom", "Warning",
            [new(MetricType.Co2, 1300, DateTimeOffset.UtcNow, AlertLevel.Warning), new(MetricType.Noise, 55, DateTimeOffset.UtcNow)]);
        var card = RoomCard.FromResponse(response);
        Assert.Equal(id, card.Id);
        Assert.Equal("101", card.Name);
        Assert.Equal("Попередження", card.StatusText);
        Assert.Equal("CO₂: 1300 ppm · Шум: 55 dB(A)", card.MetricsText);
    }

    [Fact]
    public void MissingMeasurementsDoNotClaimNormalConditions()
    {
        var response = new RoomSummaryResponse(Guid.NewGuid(), Guid.Empty, "", "101", 1, null, "Normal", []);
        var card = RoomCard.FromResponse(response);
        Assert.Equal("Немає даних", card.StatusText);
        Assert.Empty(card.MetricsText);
    }
}
