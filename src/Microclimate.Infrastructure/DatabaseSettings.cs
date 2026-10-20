namespace Microclimate.Infrastructure;

/// <summary>Ключі конфігурації та назви провайдерів БД (docs/db-schema.md, розділ 6).</summary>
public static class DatabaseSettings
{
    public const string ProviderKey = "Database:Provider";
    public const string ConnectionStringKey = "Database:ConnectionString";

    public const string Sqlite = "Sqlite";
    public const string Postgres = "Postgres";

    // Лише для dotnet ef: генерація міграцій і локальна БД розробника.
    public const string DesignTimeSqliteConnection = "Data Source=microclimate.db";
    public const string DesignTimePostgresConnection = "Host=localhost;Database=microclimate";
}
