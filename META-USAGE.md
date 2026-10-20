# Використання метапрограмування та ШІ

## Учасник 1

- **Інструменти:** Claude Code у застосунку Claude для Windows; .NET SDK 10.0.401; GitHub CLI 2.102.0; draw.io MCP для перегляду ER-діаграми.
- **Ключові запити:** скласти структуру проєкту за робочою програмою дисципліни;
- поділити роботу на трьох учасників; створити шар інфраструктури на основі 2 лабораторної,
- перевірити код на дотримання приципів DRY, SOLID, прибрати дублювання, хардкод,
- переврити сумісність з кодом інших учасників, виправити конфлікти. 

### Застосовані принципи метапрограмування

| Принцип / інструмент | Де застосовано | Переваги | Обмеження |
|---|---|---|---|
| ШІ-агент для генерації коду й документації | Сутності Domain, порти Application, тести, docs/db-schema.md, docs/analytics.md, docs/simulator.md | Швидкий узгоджений результат за детальним описом задачі; агент сам перевіряв збірку й тести | Генерація не доводить коректності, агент може помилитися в нормах і числах. Тому неперевірені норми позначено «[перевірити]», а розрахунки мають приклади, які можна перерахувати вручну |
| Data-driven тести xUnit (`[Theory]`, `[InlineData]`, `[MemberData]`) | tests/Microclimate.Domain.Tests | Один тест перевіряє багато межових значень | Помилка в даних тесту може сховати помилку коду, тому дані звірено з docs/analytics.md |
| Діаграми як код (Mermaid) | ER-діаграма в docs/db-schema.md | Діаграма зберігається в git разом з кодом, GitHub її показує | Мало контролю над розташуванням елементів |
| Генерація міграцій dotnet ef | src/Microclimate.Infrastructure/Persistence/Migrations | DDL для SQLite і PostgreSQL створюється з моделі EF Core, без ручного SQL | Згенерований код треба переглядати; у ньому вимкнено аналізатори (.editorconfig у папці міграцій) |

### Написані та згенеровані артефакти

| Файл  | Вміст | 
|---|---|---|---|
| docs/db-schema.md | Таблиці, обмеження, індекси, 3НФ, транзакції, провайдери, ER-діаграма | 
| docs/analytics.md | Пороги з посиланнями, агрегати, індекс комфорту з прикладом, сповіщення, 15 тест-кейсів |
| docs/simulator.md | Модель значень, сценарії, конфігурація, будова |
| src/Microclimate.Domain/** | Сутності, enum-и, value objects, MetricLimits, DefaultThresholds |
| tests/Microclimate.Domain.Tests/** | Тести перевірок і межових значень | Оновлено під змінену модель | Усі тести проходять локально; CI — у PR |
| src/Microclimate.Application/** | Інтерфейси репозиторіїв, UoW, аналітики, прийому вимірів, експорту, IAlertNotifier | Склад інтерфейсів змінено за рішенням команди | Release build 0 warnings; 3 тести |
| src/Microclimate.Infrastructure/** (крім міграцій) | AppDbContext, контексти SQLite і PostgreSQL, конфігурації таблиць, репозиторії, AddInfrastructure |
| src/Microclimate.Application/Analytics/** (реалізації) | ComfortIndexCalculator, AnalyticsService, AggregationBuckets |
| DECISIONS.md | Формулювання рішень і декларація |


## Учасник 2

- **Інструменти:** Gemini 3.8 Flash (Antigravity AI Assistant), Visual Studio Code / JetBrains Rider, .NET CLI 10.0.401, Git.
- **Принципи та механізми (мета)програмування:**
  1. *Декларативне та атрибутне програмування:* використання метаданих і C#-атрибутів (`[ApiController]`, `[ApiVersion("1.0")]`, `[Route("...")]`, `[ProducesResponseType<T>]`) для автоматичного формування маршрутизації, версіонування та фільтрації відповідей без імперативного коду.
  2. *Compile-time кодогенерація OpenAPI:* генерація OpenAPI-документації v1 за допомогою source generators бібліотеки `Microsoft.AspNetCore.OpenApi` на основі статичних типів контролерів та DTO.
  3. *Primary Constructors та Records для незмінних DTO:* `public sealed record` з позиційними параметрами забезпечують автоматичну генерацію методів еквівалентності (`Equals`, `GetHashCode`), форматування `ToString` та копіювання (`with`), зменшуючи обсяг бойлерплейт-коду.
  4. *ШІ-асистент для проектування контрактів і тестів:* генерація специфікацій REST API (`docs/api.md`), моделі безпеки (`docs/security.md`), контрактів SignalR (`docs/realtime.md`), типізованих DTO в `Microclimate.Contracts` та інтеграційних тестів WebApplicationFactory.

### Переваги та обмеження підходів

| Підхід / Інструмент | Переваги | Обмеження |
|---|---|---|
| **Атрибутне програмування (`[ApiVersion]`, `[ProducesResponseType]`)** | Декларативність, читабельність, єдине джерело правди для Swagger та маршрутизації. | Помилки конфігурації виявляються під час запуску або аналізаторами; обмежена гнучкість для динамічних маршрутів. |
| **C# Records (`record`) для DTO** | Лаконічність (1-2 рядки на DTO), вбудована незмінність (immutability), безпека при передачі в багатопотоковому середовищі. | Відсутність традиційного наслідування для складних ієрархій; потребує суворого узгодження імен полів при десеріалізації. |
| **Генерація OpenAPI .NET 10** | Відсутність важкої рефлексії Swashbuckle у runtime, висока швидкість cold-start сервісу. | Потребує точного узгодження версій NuGet (`Microsoft.AspNetCore.OpenApi` та `Asp.Versioning.OpenApi`). |
| **ШІ-асистент (Antigravity / Gemini)** | Швидке проектування специфікацій, таблиць ендпоінтів, покриття контрактів коментарями та розробка інтеграційних тестів. | Ризик генерації надлишкових допоміжних сутностей; згенерований код вимагає обов'язкової компіляції (`TreatWarningsAsErrors`), перевірки тестами та людського рев'ю. |

### Ключові запити до ШІ-асистента
- *«Сформувати специфікацію REST API v1, формат ProblemDetails (RFC 9457) та діаграми взаємодії у docs/api.md».*
- *«Створити модель безпеки у docs/security.md: ролі, матриця прав, життєвий цикл JWT, ротація refresh-токенів, X-Device-Key».*
- *«Спроєктувати події реального часу SignalR у docs/realtime.md та HubMethods».*
- *«Реалізувати чистий набір DTO у Microclimate.Contracts згідно з розділом 4.1 методички без зайвих залежностей».*
- *«Підняти хост Api з версіонуванням, Swagger UI, GlobalExceptionHandler та написати 5 інтеграційних тестів WebApplicationFactory».*

### Згенеровані артефакти та верифікація

| Файл / Артефакт | Що згенеровано агентом | Що змінено вручну / скориговано | Спосіб перевірки |
|---|---|---|---|
| `docs/api.md` | Таблиця ендпоінтів v1, одиниці вимірювань, ProblemDetails, дві Mermaid sequence-діаграми | Погоджено структуру з методичкою та вимогами команди | Перегляд Markdown, валідація Mermaid, рев'ю Учасниками 1 і 3 |
| `docs/security.md` | Ролі, матриця доступу, політика ротації refresh-токенів, обґрунтування X-Device-Key та CORS | Узгоджено вимоги до сутності RefreshToken з Учасником 1 | Аналітичний перегляд моделі загроз, відповідність RFC 7519 |
| `docs/realtime.md` | Специфікація SignalR: хаб `/hubs/alerts`, групи кімнат, події, схема автоматичного реконекту | Узгоджено формат payload із DTO клієнта | Звірка назв подій із `HubMethods`, валідація діаграми |
| `src/Microclimate.Contracts/` (36 файлів) | Усі DTO та публічні переліки для Common, Auth, Users, Buildings, Rooms, Sensors, Measurements, Analytics, Thresholds, Alerts, Realtime, Service | Видалено зайві експериментальні типи (`RoomStatus`, `RoomType`), спрощено сигнатури | `dotnet build src/Microclimate.Contracts -c Release` (0 попереджень, 0 помилок) |
| `src/Microclimate.Api/Program.cs` | Конфігурація хоста: версіонування URL-сегментом, SwaggerUI, HealthChecks, TimeProvider, GlobalExceptionHandler | Узгоджено налаштування OpenAPI з аналізатором `Asp.Versioning` | Збірка та інтеграційні тести |
| `src/Microclimate.Api/Middleware/GlobalExceptionHandler.cs` | Реалізація `IExceptionHandler` з генерацією ProblemDetails та маскуванням detail у Production | Додано витягування Activity TraceId | Перевірка в інтеграційному тесті 404/500 |
| `src/Microclimate.Api/Controllers/V1/PingController.cs` | Контролер діагностики з версіонуванням 1.0 та TimeProvider | Додано атрибути `[ApiVersion("1.0")]` та `[ProducesResponseType]` | Інтеграційний тест `Ping_V1_ReturnsOkWithVersion` |
| `tests/Microclimate.Api.IntegrationTests/` | 5 інтеграційних тестів на базі `WebApplicationFactory<Program>` | Налаштовано локальний `.editorconfig` для CA1707 (рішення D-1-010) | `dotnet test tests/Microclimate.Api.IntegrationTests -c Release` (5/5 успішно) |
| `Directory.Packages.props` | Додано версії `Swashbuckle.AspNetCore.SwaggerUI`, `Asp.Versioning.Mvc`, `Asp.Versioning.OpenApi`, `Microsoft.AspNetCore.Mvc.Testing` | Оновлено `Microsoft.AspNetCore.OpenApi` до 10.0.10 для усунення downgrade-попередження | `dotnet restore`, `dotnet test Microclimate.Ci.slnf` |
| `DECISIONS.md` | Записи архітектурних рішень D-2-001 … D-2-008 | Сформульовано аргументацію та наслідки | Перегляд документації |

### Етап 1 (Реалізація API, Безпеки, Подій та Аналітики)

- **Ключові задачі Етапу 1:**
  1. *Блок 1 (Auth & Error Handling):* Реалізація `JwtTokenService` (HMAC-SHA256, 15m access token, 7d refresh token), обробника заголовка `X-Device-Key` (`DeviceKeyAuthHandler`) із SHA-256 хешуванням, `AuthController` та глобальної обробки винятків (`GlobalExceptionHandler`) за RFC 9457 ProblemDetails. Автономний `InMemoryStorage` для незалежного запуску та тестування до готовності EF Core від Учасника 1.
  2. *Блок 2 (Rooms & Sensors):* Контролери `RoomsController` та `SensorsController` з одноразовою видачею `DeviceKey`, ротацією ключів `RotateDeviceKey` та валідацією залежностей (заборона видалення кімнати з прив'язаними сенсорами).
  3. *Блок 3 (Measurements, Thresholds, Alerts & Realtime):* Пакетна реєстрація показників датчиків (до 500 штук за виклик) із перевіркою фізичних меж і часового зсуву (clock skew ±5 хв/7 днів) у `MeasurementIngestionService`. Реалізація `AlertsHub` на базі SignalR (кімнатні підписки `room-{id}` та `all-alerts`), `ThresholdsController`, `AlertsController` з можливістю підтвердження тривог (`AcknowledgeAlert`) та потоковий експорт у CSV.
  4. *Блок 4 (Users & Analytics):* Адміністративний `UsersController` з керуванням ролями, математичний калькулятор індексу комфорту `ComfortIndexCalculator` (відповідно до формули розділу 3 `docs/analytics.md` з ваговими коефіцієнтами T:0.3, CO₂:0.3, RH:0.2, PM2.5:0.2 та критичним обнуленням) та аналітичні сервіси `AnalyticsController` (summary, series, comfort).
- **Верифікація:**
  - 42 інтеграційні тести WebApplicationFactory у `tests/Microclimate.Api.IntegrationTests` (загалом 86 тестів у солюшені, 100% успішні).
  - Компіляція Release 0 Warning(s), 0 Error(s) із увімкненим `TreatWarningsAsErrors`.

> **Примітка для захисту:** Учасник 2 володіє структурою коду, алгоритмом обчислення комфорту, життєвим циклом JWT/DeviceKey та ланцюжком обробки помилок через ProblemDetails.


## Учасник 3

- **Інструменти:** OpenAI Codex (GPT-6), JetBrains Rider 2026.2.3.1, .NET CLI 10.0.300.
- **Запит:** виконати Етап 0 за participant-3-stage-0.md, під’єднати katyadn/mppSTEM у Rider; встановити залежності та самостійно виконати технічну роботу.
- **Шаблони dotnet new:** створили solution, бібліотеки, API, worker, MAUI і xUnit-проєкти; перевага — стандартна структура, обмеження — демонстраційний код і залежності треба перевіряти.
- **ШІ:** створив каркас, конфігурацію та документацію; перевага — швидкість і узгодженість, обмеження — генерація не доводить коректність, потрібні збірка, тести та рев’ю.
- **CommunityToolkit.Mvvm 8.4.2:** `[ObservableProperty]` генерує властивості IsBusy/Title з повідомленнями змін; `[RelayCommand]` генерує ICommand з методів. Переваги — менше повторюваного коду і async commands; обмеження — потрібні partial-класи та перевірка згенерованих імен і binding на збірці.

| Артефакти | Що згенеровано/змінено агентом | Що людина змінила вручну | Перевірка |
|---|---|---|---|
| .github/workflows/ci.yml | Linux build/test/coverage та Windows MAUI job; версії Actions перевірено через GitHub releases API | Не підтверджено | Фактичний статус GitHub Actions у підсумковому звіті |
| .github/ISSUE_TEMPLATE/, .github/pull_request_template.md, CONTRIBUTING.md | Шаблони задач і PR, правила команди | Не підтверджено | YAML-структура та перегляд тексту |
| README.md, docs/responsibility-zones.md, deploy/README.md | Опис системи, зон і запуску за ОС; Docker відкладено до Етапу 2 | Не підтверджено | Швидкий старт серверної частини на чистому клоні; результати в підсумковому звіті |
| Client.Maui/ViewModels/, Services/, Views/, AppShell, App, MauiProgram | П’ять MVVM-екранів, source generators, compiled bindings, DI й маршрути | Не підтверджено | Платформний build, CI Windows і ручна навігація; фактичні результати в чеклісті |
| docs/ui/wireframes.md, docs/ui/screenshots/stage-0/README.md | П’ять ASCII-макетів, ролі/дії/навігація та інструкція для реальних скриншотів | Погодження командою ще не підтверджено | Перегляд змісту; скриншоти працюючого UI перевіряти окремо |
| docs/stage-0-checklist.md | Статуси issues/PR, фактичні перевірки, блокери й сім питань до захисту | Людське рев’ю ще не підтверджено | Зіставлено з результатами CLI та GitHub Actions |
| GitHub issues #1–#7 та PR #8–#14 | Формулювання задач, критерії готовності, описи змін і перевірок | Людина надала завдання та дозвіл; командне рев’ю ще очікується | Посилання і статуси перевірено через GitHub API та gh |
| global.json, .editorconfig, .gitignore | Шаблони та правила для SDK, стилю й артефактів | Не підтверджено | dotnet CLI, git diff |
| src/, tests/, Microclimate.slnx | Порожні проєкти, стандартні API/worker/MAUI, залежності й чотири заглушки | Не підтверджено | Release build і dotnet test; результати в підсумковому звіті |
| Directory.Build.props, Directory.Packages.props, Microclimate.Ci.slnf | Спільні параметри, CPM, виправлена OpenAPI-залежність і CI-фільтр | Не підтверджено | Restore, NuGet Audit і build |
| DECISIONS.md, META-USAGE.md | Рішення й декларація генерації | Потребують перегляду Учасником 3 | Перевірка відповідності фактичним діям |

**Що ШІ не реалізував:** предметні сутності, DTO, контролери системи мікроклімату, EF Core, аналітику, HTTP-клієнт і справжню автентифікацію. WeatherForecast та Worker — стандартні демонстраційні шаблони, які замінять відповідальні учасники.

**Що зробила людина:** надала завдання, вибрала репозиторій, дозволила встановлення інструментів. Людина також окремо дозволила OAuth-вхід GitHub CLI та завершила авторизацію у браузері. Подальше ручне редагування, рев’ю, погодження макетів і Windows-запуск не вважаються виконаними без підтвердження.

Перед захистом Учасник 3 має переглянути цей розділ і викласти пояснення своїми словами. Згенеровані заглушки не є доказом реалізованої бізнес-логіки.

### Фактична перевірка та ключові уточнення

- Restore і Release build CI-фільтра: успіх, 0 warnings/0 errors. Чотири тестові заглушки проходять. Звіт покриття створено; предметного покриття це не доводить.
- Запит до агента уточнено: користувач доручив самостійно встановити інструменти та виконати всю технічну роботу. Установлено MAUI, GitHub CLI і Docker Desktop; GitHub операції використовують уже збережені облікові дані. Перші операції не вимагали нових GitHub дозволів. Пізніше людина окремо погодила OAuth-доступ GitHub CLI та завершила авторизацію у браузері; CLI підтвердив постійний вхід як dimatsenko.
- Помилки генерації виправлено: NuGet Audit/OpenAPI 2.0.0 → 2.7.5, конфлікт Logging.Debug → 10.0.8, шаблонний лог-параметр Worker `{time}` → `{Time}`. Ім’я Placeholder_ShouldPass має вузько обґрунтований виняток CA1707 за вимогою завдання.
- Платформне налаштування MauiTargetFramework виправлено після виявлення поширення глобального TargetFrameworks на Contracts; тепер Contracts залишається net10.0.
- Windows CI pipeline успішний; статус фінального MAUI-каркасу дивитися у PR. macOS C#/XAML скомпілювалися з preview SDK, але повний build/run завершився помилкою native linker через Xcode/SDK; Windows UI-виконання не перевірено.
- Швидкий старт на чистому клоні, фінальні CI statuses та ручні перевірки фіксуються в docs/stage-0-checklist.md. До людського перегляду всі ручні правки й погодження позначаються як непідтверджені.

- Завершальна перевірка: чистий GitHub-клон commit bade40a пройшов restore, Release build із 0 warnings/0 errors і всі чотири тести. Фінальний код MAUI commit f6b68b7 має успішні Linux і Windows jobs у run 38288647440. Після цього оновлено Markdown-звіт та прибрано зайві пробіли стандартних шаблонів; реалізація не змінювалася.
- Rider відкрив Microclimate.slnx і визначив /Users/dmytro/.dotnet/dotnet, SDK 10.0.300. Робочий клон: ~/RiderProjects/mppSTEM, feature/7-stage0-docs.

- Додаткова перевірка README: локальний `dotnet run --project src/Microclimate.Api --launch-profile http` запустив стандартний API на localhost:5195; GET /WeatherForecast повернув HTTP 200 і п’ять шаблонних записів. Тестовий процес завершено. Команди чистого старту містять checkout повної feature-гілки до restore, доки main очікує merge.
