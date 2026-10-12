namespace Microclimate.Contracts.Common;

/// <summary>
/// Загальний статус якості мікроклімату приміщення.
/// </summary>
public enum RoomStatus
{
    /// <summary>Усі показники в межах норми.</summary>
    Normal = 1,

    /// <summary>Зафіксовано попереджувальне відхилення.</summary>
    Warning = 2,

    /// <summary>Зафіксовано критичне відхилення.</summary>
    Critical = 3,
}
