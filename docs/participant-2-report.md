# Звіт до захисту: Учасник 2

Зона: `Microclimate.Api`, `Microclimate.Contracts`, JWT/DeviceKey, SignalR, ProblemDetails, OpenTelemetry та API integration tests. Опис звірено з реалізацією 15.11.2026.

## Основні зміни

| Робота | Issue / гілка | PR і дата merge |
|---|---|---|
| Специфікації, DTO, хост, початкові тести | #23, #24, #32, #35–#39; `feature/39-stage0-docs` | [#40](https://github.com/katyadn/mppSTEM/pull/40), 12.10 |
| Auth, rooms/sensors, measurements, thresholds/alerts, users/analytics | `feature/etap1-users-analytics` | [#52](https://github.com/katyadn/mppSTEM/pull/52), 20.10 |
| Підключення API до БД — спільна інтеграція, PR Учасника 1 | #53; `feature/53-api-database` | [#54](https://github.com/katyadn/mppSTEM/pull/54), 20.10 |
| Декларація використаних інструментів | `docs/update-meta-usage` | [#68](https://github.com/katyadn/mppSTEM/pull/68), 24.10 |
| Звіт і OpenTelemetry | `docs/participant-2-report` | [#75](https://github.com/katyadn/mppSTEM/pull/75), 15.11 |

Номери issues не є номерами PR. OpenTelemetry у `Program.cs` збирає ASP.NET Core traces/metrics; Console exporter увімкнений лише в Development, production exporter не налаштований.

## Приймальні та крайові сценарії

| Сценарій | Реальний тест у Api.IntegrationTests |
|---|---|
| Вхід повертає access/refresh і TTL 900 с | `AuthTests.Login_ValidCredentials_ReturnsTokens` |
| Неправильний пароль → 401; email → 400 | `AuthTests.Login_InvalidPassword_Returns401`, `Login_InvalidEmail_Returns400ValidationProblem` |
| Без JWT профіль недоступний; JWT дає профіль | `AuthTests.Me_WithoutToken_Returns401`, `Me_WithBearerToken_ReturnsUserProfile` |
| Чинний refresh → нові токени; некоректний → 401 | `AuthTests.Refresh_ValidRefreshToken_ReturnsNewTokens`, `Refresh_InvalidToken_Returns401` |
| Operator не може створити кімнату/датчик або читати users | `RoomsTests`, `SensorsTests`, `UsersTests` — відповідні 403-сценарії |
| DeviceKey приймається; неправильний ключ → 401 | `MeasurementsTests.Ingest_WithValidDeviceKey_Returns202Accepted`, `Ingest_WithInvalidDeviceKey_Returns401Unauthorized` |
| Значення за фізичною межею → 400 | `MeasurementsTests.Ingest_ExceedingPhysicalLimit_Returns400BadRequest` |
| Summary, series, comfort; період понад 90 днів → 400 | `AnalyticsTests` |
| Неправильні пороги → 400; вимір створює тривогу, її можна підтвердити | `ThresholdsAndAlertsTests` |
| Вимір → SQLite → аналітика → експорт | `DatabaseFlowTests.IngestedMeasurement_IsStored_Summarized_AndExported` |
| Health/ping, невідомий маршрут, версія API | `ApiHostTests` |

Це тести HTTP через WebApplicationFactory; DatabaseFlowTests використовує SQLite. Реєстрації `/auth/register`, ролі User та файлу ErrorHandlingTests у реалізації немає. Повна таблиця маршрутів і відповідей — [api.md](api.md).

## Перевірки та цілісність

API має 43 integration tests; загальні результати, coverage і команди — [verification.md](verification.md). `TreatWarningsAsErrors` та nullable увімкнено; інформаційні підказки аналізатора не прирівнюються до warning. EF Core, міграції й транзакції надає Infrastructure Учасника 1.

JWT/DeviceKey реалізовані в `JwtTokenService` і `Authentication/DeviceKeyAuthHandler.cs`. Хешування паролів, refresh validation та доступ до ingest мають [описані обмеження](security.md). Перелік ролей і числові JSON enum звірено з кодом; документація не приписує API відсутніх механізмів захисту.

## Рев'ю та демонстрація

У GitHub підтверджені approval від `katyadn` на [#71](https://github.com/katyadn/mppSTEM/pull/71) і [#72](https://github.com/katyadn/mppSTEM/pull/72). Порожній approval не доводить циклу «суттєве зауваження → виправлення → повторна перевірка» для B3.

На захисті: запустити API за README, увійти через Swagger, показати права Operator/Admin, прийняти вимір, отримати аналітику й підтвердити тривогу. Далі відкрити відповідний контролер, DTO, тест і ProblemDetails. Атрибути маршрутизації, records та генерація OpenAPI описані в [META-USAGE](../META-USAGE.md); архітектурні рішення — у [DECISIONS](../DECISIONS.md).
