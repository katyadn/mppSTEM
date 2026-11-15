# C5: схема ізольованої SQLite після запуску README

Дата: 15.11.2026. Чистий клон f5e0e9e, справжні EF Core міграції. Серверні реалізації належать Учасникам 1 і 2. Це перевірка схеми; сама наявність FK не є тестом усіх можливих порушень.

| Таблиця | Стовпці PK | FK | Індекси |
|---|---|---:|---|
| Alerts | Id | 3 | IX_Alerts_Status_RaisedAt, IX_Alerts_SensorId, IX_Alerts_Room_Metric_Status, IX_Alerts_AcknowledgedByUserId, sqlite_autoindex_Alerts_1 |
| Measurements | Id | 1 | IX_Measurements_Sensor_Metric_Time |
| Rooms | Id | 0 | IX_Rooms_Name, sqlite_autoindex_Rooms_1 |
| Sensors | Id | 1 | IX_Sensors_DeviceKeyHash, IX_Sensors_SerialNumber, IX_Sensors_RoomId, sqlite_autoindex_Sensors_1 |
| Thresholds | Id | 1 | IX_Thresholds_RoomId_MetricType, sqlite_autoindex_Thresholds_1 |
| Users | Id | 0 | IX_Users_Email, sqlite_autoindex_Users_1 |

## Обмеження таблиць

```sql
CREATE TABLE "Alerts" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Alerts" PRIMARY KEY,
    "RoomId" TEXT NOT NULL,
    "SensorId" TEXT NULL,
    "MetricType" TEXT NOT NULL,
    "Level" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    "Value" REAL NOT NULL,
    "ThresholdValue" REAL NOT NULL,
    "Message" TEXT NOT NULL,
    "RaisedAt" INTEGER NOT NULL,
    "AcknowledgedAt" INTEGER NULL,
    "AcknowledgedByUserId" TEXT NULL,
    "ResolvedAt" INTEGER NULL,
    CONSTRAINT "FK_Alerts_Rooms_RoomId" FOREIGN KEY ("RoomId") REFERENCES "Rooms" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Alerts_Sensors_SensorId" FOREIGN KEY ("SensorId") REFERENCES "Sensors" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Alerts_Users_AcknowledgedByUserId" FOREIGN KEY ("AcknowledgedByUserId") REFERENCES "Users" ("Id") ON DELETE SET NULL
);
CREATE TABLE "Measurements" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Measurements" PRIMARY KEY AUTOINCREMENT,
    "SensorId" TEXT NOT NULL,
    "MetricType" TEXT NOT NULL,
    "Value" REAL NOT NULL,
    "MeasuredAt" INTEGER NOT NULL,
    CONSTRAINT "FK_Measurements_Sensors_SensorId" FOREIGN KEY ("SensorId") REFERENCES "Sensors" ("Id") ON DELETE RESTRICT
);
CREATE TABLE "Rooms" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Rooms" PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "Floor" INTEGER NOT NULL,
    "AreaM2" TEXT NULL,
    "Type" TEXT NOT NULL,
    "CreatedAt" INTEGER NOT NULL,
    CONSTRAINT "CK_Rooms_AreaM2" CHECK ("AreaM2" IS NULL OR "AreaM2" > 0),
    CONSTRAINT "CK_Rooms_Floor" CHECK ("Floor" BETWEEN -5 AND 200)
);
CREATE TABLE "Sensors" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Sensors" PRIMARY KEY,
    "RoomId" TEXT NOT NULL,
    "SerialNumber" TEXT NOT NULL,
    "Model" TEXT NULL,
    "DeviceKeyHash" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "InstalledAt" INTEGER NOT NULL,
    "LastSeenAt" INTEGER NULL,
    CONSTRAINT "FK_Sensors_Rooms_RoomId" FOREIGN KEY ("RoomId") REFERENCES "Rooms" ("Id") ON DELETE RESTRICT
);
CREATE TABLE "Thresholds" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Thresholds" PRIMARY KEY,
    "RoomId" TEXT NOT NULL,
    "MetricType" TEXT NOT NULL,
    "CriticalMin" REAL NULL,
    "WarningMin" REAL NULL,
    "WarningMax" REAL NULL,
    "CriticalMax" REAL NULL,
    CONSTRAINT "CK_Thresholds_Order" CHECK (("CriticalMin" IS NOT NULL OR "WarningMin" IS NOT NULL OR "WarningMax" IS NOT NULL OR "CriticalMax" IS NOT NULL) AND ("CriticalMin" IS NULL OR "WarningMin" IS NULL OR "CriticalMin" <= "WarningMin") AND ("WarningMin" IS NULL OR "WarningMax" IS NULL OR "WarningMin" < "WarningMax") AND ("WarningMax" IS NULL OR "CriticalMax" IS NULL OR "WarningMax" <= "CriticalMax")),
    CONSTRAINT "FK_Thresholds_Rooms_RoomId" FOREIGN KEY ("RoomId") REFERENCES "Rooms" ("Id") ON DELETE CASCADE
);
CREATE TABLE "Users" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY,
    "Email" TEXT NOT NULL,
    "DisplayName" TEXT NOT NULL,
    "PasswordHash" TEXT NOT NULL,
    "Role" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "CreatedAt" INTEGER NOT NULL
);
```

Параметризовані запити та rollback підтверджуються кодом EF Core репозиторіїв і виконаним `ExecuteInTransaction_FailedAction_SavesNothing`. Обґрунтування нормалізації — docs/db-schema.md.
