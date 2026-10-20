# Звіт до захисту: Учасник 1

Зона відповідальності: шари Domain, Application, Infrastructure (модель даних, правила, база даних, аналітика), симулятор датчиків. Структуру критеріїв узято з «Як захищати проєкт з МПП».

Як відтворити цифри зі звіту:

```bash
dotnet tool restore
dotnet build Microclimate.Ci.slnf -c Release
dotnet test Microclimate.Ci.slnf -c Release --collect:"XPlat Code Coverage"
```

## 1. Хід роботи

| Дата | Етап | Що зроблено |
|---|---|---|
| 12.10 | 0 | Схема БД (6 таблиць, ER-діаграма, 3НФ, індекси, транзакції), специфікація аналітики з нормами, дизайн симулятора |
| 12.10 | 0 | Сутності Domain з перевірками, інтерфейси Application, unit-тести, записи в DECISIONS.md і META-USAGE.md |
| 13.10 | 0 | Перегляд моделі: прибрано методи, що не використовуються, переглянуто тести |
| 19.10 | 1 | EF Core: AppDbContext, конфігурації таблиць, міграції для SQLite і PostgreSQL, репозиторії, транзакції |
| 19.10 | 1 | Аналітика: зведення за період, часовий ряд з порожніми інтервалами, індекс комфорту |
| 20.10 | 1 | Підключення Api до БД, пошук датчика за ключем, тест повного ланцюга «вимір → БД → аналітика» |
| 20.10 | 1 | Огляд якості коду: довідник метрик, обов'язкові налаштування БД, прибрано дублювання |

## 2. B1. Ланцюг issue → branch → PR → merge

| Issue | Гілка | PR | Стан |
|---|---|---|---|
| #15, #21 Схема БД, провайдери | feature/15-db-schema | #25 | злито |
| #16 Доменна модель | feature/16-domain-model | #26 | злито |
| #17 Тести домену | feature/17-domain-tests | #27 | злито |
| #18 Інтерфейси Application | feature/18-application-abstractions | #28 | злито |
| #19 Специфікація аналітики | feature/19-analytics-spec | #29 | злито |
| #20 Дизайн симулятора | feature/20-simulator-design | #30 | злито |
| #22 Рішення та META-USAGE | feature/22-stage0-docs | #31 | злито |
| #41 Перегляд моделі й тестів | feature/41-revise-domain | #42 | злито |
| #49 Аналітика | feature/49-analytics-service | #50 | злито |
| #48 Infrastructure | feature/48-infrastructure | #51 | злито |
| #55 Якість коду | feature/55-code-quality | #56 | злито |
| #53 Підключення Api до БД | feature/53-api-database | #54 | очікує злиття PR #52 Учасника 2 |

Кожен коміт містить номер issue. Рішення з обґрунтуванням: DECISIONS.md, записи D-1-001…D-1-016.

## 3. B3. Проведені code-review

| PR (автор) | Знайдена проблема або пропозиція | Реакція автора | Перевірено |
|---|---|---|---|
| | | | |

Кандидати для рев'ю PR #52 (Учасник 2): хешування паролів SHA-256 без солі; невикористані сервіси аналітики в Api/Services після підключення AddApplication.

## 4. C1. Приймальні тест-кейси аналітики

Кейси з docs/analytics.md, розділ 5.

| # | Вхід | Очікувано | Як перевірено | Результат |
|---|---|---|---|---|
| AN-01 | Офіс, T = 22 °C | норма | ThresholdTests.Evaluate_OfficeTemperature_ReturnsLevel | пройдено |
| AN-02 | Офіс, T = 25 °C | норма | той самий тест | пройдено |
| AN-03 | Офіс, T = 26.5 °C | Warning, s = 50 | Warning — тест для 25.01 і 28; s = 100 · (1 − 1.5/3) = 50 — розрахунок вручну | пройдено |
| AN-04 | Офіс, T = 28 °C | Warning, s = 0 | ThresholdTests (28 → Warning) | пройдено |
| AN-05 | Офіс, T = 28.1 °C | Critical | ThresholdTests (28.01 → Critical) | пройдено |
| AN-06 | T 25.5, RH 35, CO₂ 1300, PM2.5 8 | CI = 84.55, «Прийнятно» | AnalyticsTests.Calculate_ExampleFromSpecification_Returns84_55Acceptable | пройдено |
| AN-07 | Усе в нормі, CO₂ = 1800 | «Некомфортно» | AnalyticsTests.Calculate_CriticalMetric_IsUncomfortable_AndNoInputsIsNoData | пройдено |
| AN-08 | Немає вимірів | CI = null, «Немає даних» | той самий тест | пройдено |
| AN-09 | Лише T = 25.5 °C | CI = 83.33 | Розрахунок вручну за формулою з розділу 3 | пройдено |
| AN-10 | Ряд за 3 год, дані в 1-й і 3-й | 3 інтервали, 2-й порожній | AnalyticsTests.GetSeries_HourWithoutData_ReturnsEmptyBucket | пройдено |
| AN-11 | CO₂ 1300 двічі | одне сповіщення | Прийом вимірів — сервіс Учасника 2 (MeasurementIngestionService) | перевіряється тестами Учасника 2 |
| AN-12 | Warning, далі CO₂ 1800 | Warning закрито, Critical створено | те саме | перевіряється тестами Учасника 2 |
| AN-13 | Відкрите сповіщення, далі CO₂ 900 | сповіщення закрито | те саме | перевіряється тестами Учасника 2 |
| AN-14 | Зведення без вимірів | count = 0 | AnalyticsService.GetSummaryAsync додає нульове зведення для метрик без даних | реалізовано, окремого автотесту немає |
| AN-15 | Період 91 день | помилка 400 | EntityTests.DateRange_InvalidOrTooLong_Throws; AnalyticsService перевіряє період | пройдено |

## 5. C2. Крайні випадки

| # | Пункт | Де оброблено | Доказ |
|---|---|---|---|
| 1 | Валідація введення | Перевірки в сутностях (Guard, фабричні методи Create/Record/Raise) | EntityTests, MeasurementTests |
| 2 | Порожні й некоректні дані | Порожні рядки, NaN, Guid.Empty, невідомі значення enum → DomainException | MeasurementTests.Record_InvalidValue_Throws, EntityTests |
| 3 | Винятки без аварійного завершення | DomainException, NotFoundException, ValidationException перетворюються в Api на 400/404 (ProblemDetails); транзакція відкочується при помилці | RepositoryTests.ExecuteInTransaction_FailedAction_SavesNothing |
| 4 | Межові значення | Межі порогів включні (25, 28), вологість 0 і 100, поверх −5 і 200, період 90 днів | ThresholdTests, MeasurementTests.Record_HumidityOnPhysicalBoundary_IsAllowed |
| 5 | Немає необроблених винятків у лозі прогону | Перевіряється на демонстрації: запуск Api з БД, надсилання вимірів, запити аналітики | лог прогону на захисті |

## 6. C3. Автотести й покриття

| Проєкт | Тестів | Покриття рядків |
|---|---|---|
| Microclimate.Domain.Tests | 40 | Domain — 88.6 % |
| Microclimate.Application.Tests | 6 | Application — 66.4 % (71.3 % разом з тестами Api) |
| Microclimate.Infrastructure.Tests | 4 | Infrastructure без згенерованих міграцій — 67.8 % (74.8 % разом з тестами Api) |
| DatabaseFlowTests у Microclimate.Api.IntegrationTests | 1 | ланцюг «датчик → Api → SQLite → аналітика» |

Тести на xUnit: [Fact] — один сценарій, [Theory] з [InlineData] — межові значення. Час передається параметром, тому тести детерміновані. Репозиторії тестуються на SQLite в пам'яті з тими самими міграціями, що й у робочій БД. CI запускає тести на кожен pull request і збирає звіт покриття (coverlet, ReportGenerator).

## 7. C4. Статичний аналіз

- Directory.Build.props: AnalysisLevel latest-recommended, EnforceCodeStyleInBuild, TreatWarningsAsErrors — будь-яке зауваження аналізатора зупиняє збірку.
- Release-збірка: 0 warnings, 0 errors.
- Згенерований код міграцій виключено з аналізу (.editorconfig у папці Migrations).
- NuGet Audit увімкнено: вразливу транзитивну залежність SQLitePCLRaw замінено виправленою версією (D-1-014).

## 8. C5. Цілісність і безпека даних

| # | Пункт | Як виконано | Де |
|---|---|---|---|
| 1 | Нормалізація ≥ 3НФ | 6 таблиць у 3НФ; кімната виміру визначається через датчик, не дублюється | docs/db-schema.md, розділ 5 |
| 2 | PK/FK та обмеження | Первинні й зовнішні ключі з RESTRICT/CASCADE/SET NULL, UNIQUE (назва кімнати, серійний номер, email), CHECK (поверх, площа, порядок меж порогу) | Persistence/Configurations/EntityConfigurations.cs, міграції |
| 3 | Індекси на полях пошуку/JOIN | Measurements (SensorId, MetricType, MeasuredAt), Sensors (RoomId, DeviceKeyHash), Alerts (Status, RaisedAt) і (RoomId, MetricType, Status) | те саме |
| 4 | Параметризовані запити | Усі запити через EF Core LINQ, без ручного SQL | Repositories/ |
| 5 | Транзакції | IUnitOfWork.ExecuteInTransactionAsync: усе або нічого | Persistence/AppDbContext.cs, тест транзакції |

## 9. Як показати БД (ЛР1)

```bash
dotnet run --project src/Microclimate.Api --launch-profile http
```

Після старту з'являється src/Microclimate.Api/microclimate.db з таблицями й демо-даними (лише в середовищі Development). Файл відкривається в DB Browser for SQLite або DBeaver. Перемикання на PostgreSQL — змінні Database__Provider=Postgres і Database__ConnectionString.

## 10. Використані інструменти

Декларація інструментів, ключових запитів і згенерованих артефактів — META-USAGE.md, розділ «Учасник 1».
