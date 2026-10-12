namespace Microclimate.Contracts.Common;

/// <summary>
/// Призначення приміщення (впливає на рекомендовані санітарні пороги).
/// </summary>
public enum RoomType
{
    /// <summary>Офісне приміщення.</summary>
    Office = 1,

    /// <summary>Навчальна аудиторія / клас.</summary>
    Classroom = 2,

    /// <summary>Лабораторія.</summary>
    Laboratory = 3,

    /// <summary>Житлове приміщення.</summary>
    Residential = 4,

    /// <summary>Серверна кімната / ЦОД.</summary>
    ServerRoom = 5,

    /// <summary>Інше приміщення.</summary>
    Other = 6,
}
