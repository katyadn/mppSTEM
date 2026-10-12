namespace Microclimate.Domain.Enums;

/// <summary>Роль користувача системи.</summary>
public enum UserRole
{
    /// <summary>Повне керування будівлями, кімнатами, датчиками й користувачами.</summary>
    Admin = 1,

    /// <summary>Перегляд, пороги, імпорт/експорт, підтвердження сповіщень.</summary>
    Operator = 2,

    /// <summary>Лише перегляд.</summary>
    Viewer = 3,
}
