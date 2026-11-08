# Звіт до захисту: Учасник 2

Зона відповідальності: Web API (`src/Microclimate.Api`), Контракти (`src/Microclimate.Contracts`),
JWT + X-Device-Key автентифікація, ProblemDetails обробка помилок, інтеграційні тести.
Структуру критеріїв узято з «Як захистити проєкт з МПП».

Як відтворити цифри зі звіту:

```bash
dotnet tool restore
dotnet build Microclimate.Ci.slnf -c Release
dotnet test Microclimate.Ci.slnf -c Release --collect:"XPlat Code Coverage"
```

## 1. Хід роботи

| Дата  | Етап | Що зроблено |
|-------|------|-------------|
| 12.10 | 0 | Специфікація REST API v1: ендпоінти, формат помилок ProblemDetails, sequence-діаграми (#23) |
| 12.10 | 0 | Контракти DTO: auth, users, rooms, sensors, measurements, analytics, threshold, alert, realtime (#24) |
| 12.10 | 0 | Модель безпеки: JWT Bearer, X-Device-Key, матриця доступу (Operator/Admin) (#32) |
| 12.10 | 0 | Специфікація SignalR хабу: події, групи підписок, схема (#36) |
| 12.10 | 0 | Документи рішень, META-USAGE Учасника 2 (#39 → PR #40) |
| 20.10 | 1 | Скелет API хоста: Asp.Versioning, ProblemDetails, OpenAPI/Swagger з JWT Bearer (#37) |
| 20.10 | 1 | Інтеграційні тести: health, ping, обробка помилок (#38) |
| 20.10 | 1 | Контролери: Rooms, Sensors (генерація + перевірка X-Device-Key); Measurements + ingestion service; Thresholds, Alerts, SignalR AlertsHub; Users, Analytics (#52) |
| 20.10 | 1 | In-memory репозиторії: RoomRepository, SensorRepository, MeasurementRepository, AlertRepository, UserRepository, UnitOfWork |
| 20.10 | 1 | Перемикання API на БД: Database:Provider=Sqlite/Postgres, тести повного ланцюжка на SQLite (#53 → PR #54) |
| 08.11 | 2 | Оновлення META-USAGE (PR #68) |

## 2. B1. Ланцюг issue → branch → PR → merge

| Issue | Гілка | PR | Стан |
|-------|-------|----|------|
| #23 Специфікація API | feature/23-api-spec (docs) | #23 | злито |
| #24 Контракти DTO | feature/24-contracts (рефакторинг + розширення) | #24 | злито |
| #32 Модель безпеки | feature/32-security-model (docs) | #32 | злито |
| #36 Специфікація SignalR | feature/36-realtime-spec (docs) | #36 | злито |
| #39 Рішення Учасника 2 | feature/39-stage0-docs | #40 | злито |
| #37 Скелет API | feature/etap1-auth-jwt (разом із #38) | — | злито в #52 |
| #52 Контролери (всі) | feature/etap1-users-analytics (накопичувальна) | #52 | злито |
| #53 API + БД | feature/53-api-database | #54 | злито |
| #68 META-USAGE | docs/update-meta-usage | #68 | злито |

## 3. B3. Code review

| Гілка | Коміти | Знайдені зауваження | Результат |
|-------|--------|---------------------|-----------|
| `feature/69-sensor-simulator` (Учасник 1) | 3 | Немає критичних. `SimulatorWorker` коректно зупиняє конкретний датчик при 401, не весь процес. `PeriodicTimer` + `TimeProvider` — хороше рішення для тестованості. `SensorSignalGenerator` — детерміновано (seed), перегрів логічно: зростання → тримання → нормальне значення. Тести охоплюють seed-детермінізм, overheat-сценарій, валідацію конфігурації і HTTP-маппінг кодів статусу. Доповнення `SimulatorOptions` через `IOptions<T>` + валідатор при старті — правильно. | Схвалено ✓ |
| `feature/70-measurement-export` (Учасник 1) | 2 | Немає критичних. Потоковий запис (рядок за рядком для CSV, `Utf8JsonWriter` з flush-порогом для JSON) — уникає накопичення у пам'яті. `IMeasurementExporter` розміщено в Application.Abstractions, реалізація — в Infrastructure: відповідає Clean Architecture. `AddMeasurementExport()` винесено окремим методом — зручно для in-memory режиму API. `Enum.IsDefined` для валідації `?format=` — коректно. `leaveOpen: true` у `StreamWriter` — потік відповіді не закривається передчасно. | Схвалено ✓ |

## 4. C1. Приймальні тест-кейси

| # | Сценарій | Вхід | Очікуваний результат | Де перевіряється |
|---|----------|------|----------------------|------------------|
| 1 | Реєстрація + логін | POST /auth/register, POST /auth/login | 200 + JWT токен | `AuthTests.cs` |
| 2 | Доступ до захищеного ресурсу без JWT | GET /rooms без токена | 401 Unauthorized + ProblemDetails | `AuthTests.cs` |
| 3 | Доступ із JWT (роль User) до admin-ресурсу | GET /users з JWT User | 403 Forbidden + ProblemDetails | `AuthTests.cs` |
| 4 | Прийом виміру з правильним X-Device-Key | POST /measurements + заголовок | 202 Accepted | `MeasurementsTests.cs` |
| 5 | Прийом виміру з неправильним X-Device-Key | POST /measurements + невірний ключ | 401 Unauthorized | `MeasurementsTests.cs` |
| 6 | Вимір з недопустимими даними | POST /measurements, value=NaN | 400 Bad Request + ProblemDetails | `MeasurementsTests.cs` |
| 7 | Аналітика за існуючою кімнатою | GET /rooms/{id}/analytics | 200 + JSON | `AnalyticsTests.cs` |
| 8 | Аналітика за неіснуючою кімнатою | GET /rooms/{nonexistent}/analytics | 404 Not Found + ProblemDetails | `AnalyticsTests.cs` |
| 9 | Поріг спрацьовує й Alert з'являється | POST measurement > threshold → GET /alerts | Alert у списку | `ThresholdsAndAlertsTests.cs` |
| 10 | Повний ланцюжок через реальну БД | Реєстрація → вимір → БД → аналітика | Дані в SQLite | `DatabaseFlowTests.cs` |

## 5. C2. Межові та від'ємні сценарії

| # | Вхід | Очікуваний результат | Де |
|---|------|----------------------|-----|
| 1 | Значення метрики на межі допустимого діапазону (напр. T=−40 або T=+60) | 400 + деталі про поле | `MeasurementsTests.cs` |
| 2 | Пакет вимірів з дублікатом метрики | 400 Validation Error | `MeasurementsTests.cs` |
| 3 | `from` > `to` у запиті аналітики | 400 Bad Request | `AnalyticsTests.cs` |
| 4 | Невідомий `format` при експорті | 400 Bad Request | `MeasurementsTests.cs` (контролер) |
| 5 | Запит до неіснуючого маршруту | 404 + ProblemDetails (не HTML) | `ErrorHandlingTests.cs` |
| 6 | Невалідний JSON у тілі | 400 + ProblemDetails | `ErrorHandlingTests.cs` |
| 7 | Поріг Alert → ack → Alert не підіймається повторно | Стан Acknowledged, новий Alert не створюється до resolve | `ThresholdsAndAlertsTests.cs` |
| 8 | Аналітика без вимірів у БД | 200 з порожніми серіями | `DatabaseFlowTests.cs` |

## 6. C3. Автотести і покриття

Запуск: `dotnet test Microclimate.Ci.slnf -c Release --collect:"XPlat Code Coverage"`

| Проєкт тестів | Тестів | Passed | Failed | Покриття рядків (Microclimate.Api.IntegrationTests) |
|---------------|--------|--------|--------|-----------------------------------------------------|
| Api.IntegrationTests | 43 | 43 | 0 | — |
| Domain.Tests | 40 | 40 | 0 | — |
| Application.Tests | 6 | 6 | 0 | — |
| Infrastructure.Tests | 4 | 4 | 0 | — |
| Client.Tests | 44 | 44 | 0 | — |
| **Разом** | **137** | **137** | **0** | — |

Покриття по шарах (звіт Integration Tests):

| Шар | Рядки | Гілки |
|-----|-------|-------|
| `Microclimate.Api` | 65.2 % | 30.3 % |
| `Microclimate.Application` | 71.3 % | 27.6 % |
| `Microclimate.Contracts` | 64.9 % | 100 % |
| `Microclimate.Domain` | 86.3 % | 58.7 % |
| `Microclimate.Infrastructure` | 51.1 % | 31.8 % |

Нижче 100 % у гілках API — незакриті guard-гілки в контролерах (повторна перевірка null-room тощо). Contracts мають 100 % гілок, бо це чисті DTO без логіки.

Тест-дублі: Integration Tests піднімають реальний `WebApplicationFactory<Program>`, без моків; тести БД запускають SQLite in-memory з реальними EF Core міграціями.

## 7. C4. Статичний аналіз

- `Directory.Build.props`: `AnalysisLevel=latest-recommended`, `EnforceCodeStyleInBuild=true`, `TreatWarningsAsErrors=true` — будь-яке попередження аналізатора зупиняє збірку.
- Release-збірка: **0 warnings, 0 errors**.
- Nullable reference types: `<Nullable>enable</Nullable>` — всі типи анотовані.
- NuGet Audit: критичних вразливостей у залежностях `Microclimate.Api` / `Microclimate.Contracts` немає.
- Генерований код `LoggerMessage` у `SimulatorWorker` і часткові класи контролерів — виключені з аналізу через `[GeneratedCode]`.

## 8. C5. Цілісність і безпека даних

| # | Пункт | Як виконано | Де |
|---|-------|-------------|-----|
| 1 | JWT автентифікація | `JwtBearer`: перевірка підпису, exp, iss, aud; Bearer-схема — єдина для API | `Program.cs`, `appsettings.json` |
| 2 | X-Device-Key для датчиків | Ключ передається у заголовку `X-Device-Key`; у БД зберігається тільки SHA-256-хеш; порівняння через `DeviceKeyAuthenticator` | `src/Microclimate.Api/Authentication/DeviceKeyAuthenticator.cs` |
| 3 | Матриця доступу (RBAC) | Три ролі: `User`, `Operator`, `Admin`; кожен контролер явно декорований `[Authorize(Roles=...)]` | Контролери V1 |
| 4 | ProblemDetails RFC 9457 | `AddProblemDetails()` + `UseExceptionHandler` + `MapFallback` → ніколи не повертається HTML або трасування стеку у відповідь | `Program.cs`, `ErrorHandlingTests.cs` |
| 5 | Параметризовані запити | Усі запити через EF Core LINQ — SQL-ін'єкції виключені | `Infrastructure/Repositories/` |
| 6 | HTTPS Redirection | `app.UseHttpsRedirection()` у Production | `Program.cs` |

> **Відоме обмеження:** хешування паролів користувачів використовує SHA-256 без солі (виявлено під час Code Review у PR #52). Для продакшн-готовності рекомендовано замінити на BCrypt або ASP.NET Identity з PBKDF2. Зафіксовано в `DECISIONS.md`.

## 9. Як показати БД (ЛР 1)

```bash
dotnet run --project src/Microclimate.Api --launch-profile http
```

Після старту в Development-середовищі з'являється `src/Microclimate.Api/microclimate.db` з таблицями та демо-даними.
Файл відкривається в DB Browser for SQLite або DBeaver.
Перемикання на PostgreSQL: змінні `Database__Provider=Postgres` та `Database__ConnectionString=<рядок>`.

## 10. Використані інструменти

Декларація інструментів, ключових запитів і згенерованих артефактів — META-USAGE.md, розділ «Учасник 2».
