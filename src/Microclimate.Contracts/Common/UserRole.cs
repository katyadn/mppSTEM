namespace Microclimate.Contracts.Common;

/// <summary>
/// Роль користувача в системі для авторизації.
/// </summary>
public enum UserRole
{
    /// <summary>Адміністратор з повними правами управління.</summary>
    Admin = 1,

    /// <summary>Оператор з правами моніторингу, квитування та коригування порогів.</summary>
    Operator = 2,

    /// <summary>Спостерігач з правами лише на перегляд даних.</summary>
    Viewer = 3,
}
