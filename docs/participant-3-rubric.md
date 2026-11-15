# Учасник 3: докази за критеріями захисту

Стан: 15.11.2026. Джерело критеріїв — наданий користувачем PDF «Як захищати проєкт з МПП-2», сторінки 1–5. Це підготовка до захисту, а не виставлена викладачем оцінка. Зі слів учасника, відбувся один чекпоінт у суботу 31.10; другий не підтверджено. Зауваження викладача та сам банк питань не надані. Службові URL/дати нижче перевірено в GitHub.

## A1–A3. Процес

| Критерій | Фактичні докази | Що ще невідомо |
|---|---|---|
| A1: два чекпоінти | Учасник повідомив про один 31.10.2026; клієнтські PR #47, #63, #65, #67 були merged до цієї дати | Факт своєчасного подання всіх артефактів і другий чекпоінт; рівень не обчислено |
| A2: репозиторій | Issue-посилання у комітах; окремі гілки й PR; README; понад 5 рішень D-3 у DECISIONS.md | У доступній історії немає змістовних non-merge комітів dimatsenko за 26.10–08.11. Тому пункт «щотижня» не позначений виконаним |
| A3: зауваження | Є шаблон review-feedback; конкретних зауважень викладача в отриманих даних не знайдено | Потрібний фактичний перелік зауважень і строки; відсутність даних не прирівнюється до 100% |

Історичні строки та коміти не змінювались заднім числом. Авторство коду не прирівнюється до git blame: використання Codex задекларовано в META-USAGE.md.

## B1. Issue → branch → commit → PR → review → merge

Усі посилання стосуються `katyadn/mppSTEM`. Повний diff і список комітів доступні на сторінці кожного PR.

| Issue | Гілка | Приклад коміту | PR | Merge / review |
|---|---|---|---|---|
| [#1](https://github.com/katyadn/mppSTEM/issues/1) каркас | feature/1-solution-skeleton | d784e97, 7048be2 | [#8](https://github.com/katyadn/mppSTEM/pull/8) | Merge 12.10; формального review у GitHub немає |
| [#2](https://github.com/katyadn/mppSTEM/issues/2) CI | feature/2-ci-pipeline | f3f316c | [#9](https://github.com/katyadn/mppSTEM/pull/9) | Merge 12.10; review не підтверджено |
| [#3](https://github.com/katyadn/mppSTEM/issues/3) правила | feature/3-repo-templates | 31938e4 | [#10](https://github.com/katyadn/mppSTEM/pull/10) | Merge 12.10; review не підтверджено |
| [#4](https://github.com/katyadn/mppSTEM/issues/4) README | feature/4-readme | b06983b | [#11](https://github.com/katyadn/mppSTEM/pull/11) | Merge 12.10; review не підтверджено |
| [#5](https://github.com/katyadn/mppSTEM/issues/5) MAUI | feature/5-maui-skeleton | 7be4ffc, 61cbeb8 | [#12](https://github.com/katyadn/mppSTEM/pull/12) | Merge 12.10; review не підтверджено |
| [#6](https://github.com/katyadn/mppSTEM/issues/6) макети | feature/6-wireframes | a832fa6 | [#13](https://github.com/katyadn/mppSTEM/pull/13) | Merge 12.10; review не підтверджено |
| [#7](https://github.com/katyadn/mppSTEM/issues/7) рішення | feature/7-stage0-docs | a0fc254 | [#14](https://github.com/katyadn/mppSTEM/pull/14) | Merge 12.10; review не підтверджено |
| [#43](https://github.com/katyadn/mppSTEM/issues/43) Mac | fix/43-maccatalyst-launch | ad5cca5 | [#44](https://github.com/katyadn/mppSTEM/pull/44) | Merge 24.10; review не підтверджено |
| [#34](https://github.com/katyadn/mppSTEM/issues/34) інтеграція | feature/34-domain-alignment | 497d3ff, fd4536f | [#45](https://github.com/katyadn/mppSTEM/pull/45) | Merge 24.10; review не підтверджено |
| [#46](https://github.com/katyadn/mppSTEM/issues/46) робочі сценарії | feature/46-client-workflows | a10da93, 169e0c4 | [#47](https://github.com/katyadn/mppSTEM/pull/47) | Merge 24.10; review не підтверджено |
| [#62](https://github.com/katyadn/mppSTEM/issues/62) Xcode | codex/62-xcode-27-1 | 62a68a5 | [#63](https://github.com/katyadn/mppSTEM/pull/63) | Merge 24.10; review не підтверджено |
| [#64](https://github.com/katyadn/mppSTEM/issues/64) графік і вкладки | codex/tab-icons | a69408f, e683610 | [#65](https://github.com/katyadn/mppSTEM/pull/65) | Merge 24.10; review не підтверджено |
| [#66](https://github.com/katyadn/mppSTEM/issues/66) пояснення | codex/66-client-docstrings | 9c2a78e, 67aa63d | [#67](https://github.com/katyadn/mppSTEM/pull/67) | Merge 24.10; review не підтверджено |
| [#76](https://github.com/katyadn/mppSTEM/issues/76) завершення | feature/76-complete-client | 8632287, 4044522, 26d3fa1 | [#77](https://github.com/katyadn/mppSTEM/pull/77) | Реалізація й докази готові; людське review та merge очікуються |

Наявність merge сама по собі не підтверджує результативне рев'ю. Перевірено reviews 35 PR і всі доступні issue/review comments: формальних review від dimatsenko та коментарів у репозиторії на момент аудиту немає.

## B2–B3. Зобов'язання й review

Зведення задач усієї команди, закриття та явно записані дедлайни: [team-delivery-status.md](team-delivery-status.md). Частку «вчасно» не обчислено без узгоджених дат.

Для B3 потрібен завершений цикл: суттєва проблема/обґрунтоване покращення → реакція автора → перевірене виправлення. **Підтверджено 0 таких циклів від Учасника 3 у GitHub; для максимального рівня потрібно 3.** Автоматичне рев'ю власного PR і перегляд чужого коду без зауваження не зараховуються. 15.11 переглянуто diff PR #72 Учасника 1; підтвердженої суттєвої проблеми не виявлено, approval/коментар не публікувався. Це не вигаданий «результативний review».

## C0. Операційні системи

- MacCatalyst: нативна збірка й UI-прогін підтверджені [звітом зі скриншотами](ui/mac-smoke-2026-11-15.md).
- Windows та Android: успішні CI-збірки; вони не є демонстрацією роботи UI.
- iOS 27.2 Simulator: збірка й UI-прогін виконані, включно зі збереженням сесії після завершення процесу: [звіт і скриншоти](ui/ios-smoke-2026-11-15.md).

Показ викладачу й прийнятність симулятора як другої ОС підтверджує викладач.

## C1. Клієнтські приймальні кейси

Це конкретний набір за `docs/ui/wireframes.md`, а не вигаданий відсоток усіх командних кейсів. Формальне узгодження єдиного командного переліку ще не підтверджене. «Авто» означає виконаний тест без нативного UI, «Mac» — спостережений UI-сценарій.

| ID | Кроки й очікування | Результат / доказ |
|---|---|---|
| CL-01 | Неправильний email → помилка без HTTP; правильний Operator → Dashboard, пароль очищено | Авто `LoginValidationDoesNotSendRequestAndSuccessfulLoginClearsPassword`; Mac login пройдено |
| CL-02 | Відкрити огляд → latest-значення, час і серверний комфорт; завантажити наступну сторінку | Авто `DashboardUsesLatestValuesServerComfortAndPreservesLoadedPages`; Mac огляд пройдено |
| CL-03 | Відкрити кімнату з пропущеною годиною → розрив графіка, а не нуль | Авто `RoomDetailsUseSelectedPeriodAndPreserveChartGap`; Mac screenshot details-live-dark |
| CL-04 | Вибрати CO₂ / 7 днів → запит правильного періоду без Apply; одиниці ppm | Авто `ChangingPeriodAutomaticallyRefreshesWithoutOverwritingThresholdDraft`; Mac пройдено |
| CL-05 | Вибрати точку → дата, час, значення; новий вимір → вибір лишився на тому самому інтервалі | Mac live-selection-light: 1900 → 1850 ppm; авто перевірка стабільності серії |
| CL-06 | Operator вводить `24,5` → сервер зберігає 24.5; неправильний порядок → помилка до HTTP | Авто threshold parse + API/SQLite round-trip; Mac thresholds-dark |
| CL-07 | Змінити метрику з незбереженою температурною чернеткою → старі межі недоступні; CO₂ має 1200/1750 ppm | Авто `MetricChangeInvalidatesOldThresholdsEvenWhenNewRequestFails`; Mac thresholds-co2-light |
| CL-08 | Viewer намагається змінити/acknowledge → заборонено; HTTP 403 не очищає чинну сесію | Авто `ThresholdEditorChecksRoleAndSendsSelectedMetric`, `DeniedThresholdSavePreservesSessionAndDraft`, `AlertAcknowledgementRespectsRole` |
| CL-09 | Надіслати вимір → видимий екран оновився; відключити hub → polling; повернути → повторні підписки | SignalR/TestServer тести; Mac живі графік і тривога пройдені |
| CL-10 | Вибрати рівень/стан → правильні query; acknowledge Open → кнопка зникає | Авто `AlertsPassSelectedFiltersToServer`; Mac acknowledge пройдено |
| CL-11 | Вибрати світлу тему → зберегти → перезапустити → тема збережена | Авто `ThemeIsSavedWithoutSigningOutAndRestoredIntoPicker`; Mac перезапуск пройдено |
| CL-12 | Logout / зміна сервера / 401 → Login, сесія й watcher припинені | Авто session/realtime тести; Mac зміна сервера → Login пройдено |

Скриншоти й середовище: [Mac UI-звіт](ui/mac-smoke-2026-11-15.md), [iOS UI-звіт](ui/ios-smoke-2026-11-15.md). На iOS додатково пройдені login, dashboard, дотик графіка, save thresholds, persistent session, live refresh і logout; це не повтор усіх 12 кейсів на кожній платформі. TRX усіх тестів — artifact `test-results` у CI.

## C2. П'ять крайніх випадків

| Пункт рубрики | Випадок | Оброблено / доказ |
|---|---|---|
| Валідація вводу | Email, insecure remote URL, NaN/Infinity, переплутані пороги | Так; ClientWorkflowTests, CompletionWorkflowTests, ApiAddress tests |
| Порожні/некоректні дані | NoData окремо від 0; null/Count=0 розриває серію; недійсний JSON показує помилку | Так; клієнтські presentation/transport тести |
| Винятки без crash | 401, 403, 500, недоступний API/hub, скасування запиту при виході | Так; BaseViewModel, HTTP/SignalR тести, нативний прогін |
| Межові значення | Односторонні межі; кома; рівність нижньої й верхньої меж; токен на границі TTL | Так; threshold theory та session tests |
| Лог прогону | API старт → login → thresholds → ingest → analytics → acknowledge | [Чистий клон і аналіз логу](evidence/2026-11-15/fresh-start.md): немає fail/crit/Unhandled/exception у цьому API-прогоні |

## C3. Автотести й справжнє покриття

[CI 9825a37](https://github.com/katyadn/mppSTEM/actions/runs/43655472282): 168/168 тестів, із них 67 клієнтських. Попередній [CI 26d3fa1 з тим самим кодом](https://github.com/katyadn/mppSTEM/actions/runs/43654939908) дав такі значення ReportGenerator:

| Обсяг | Рядки | Гілки |
|---|---:|---:|
| Client.Core | 83.4% | 65.8% |
| Усі 7 виміряних збірок CI | 71.8% (5942/8265) | 53.6% (794/1481) |

MAUI UI не входить у ці 7 збірок; звіт включає згенеровані JSON-метадані та міграції. Denominator не змінювався для досягнення порога. **C3 у PDF є командним: 83.4% одного Client.Core не означає ≥80% усього продукту.** При використанні поточного сумарного показника 71.8% поріг максимального рівня ще не досягнуто. HTML/Markdown — artifact `coverage-report`; новий запуск може незначно змінити цифри, на захисті відкривати актуальний звіт.

## C4. Статичний аналіз

У чистому клоні commit f5e0e9e виконано Release build з .NET SDK 10.0.402, `AnalysisLevel=latest-recommended`, `EnforceCodeStyleInBuild=true`, `TreatWarningsAsErrors=true`. Результат: **0 активних error, 0 warning**, 102 інформаційні note. 139 suppressed-діагностик Client.Core походять із CommunityToolkit generated code (CS0618/CS8620); сирі SARIF їх зберігають. Це не 139 активних помилок і не заява, що analyzer не має жодних підказок.

[Підсумок 13 проєктів](evidence/2026-11-15/static-analysis.md); [окрема нативна iOS-збірка](evidence/2026-11-15/ios-static-analysis.md) також має 0 активних error/warning. CI зберігає artifact `static-analysis`: SARIF 2.1 на кожен проєкт, build.log і summary.md. Severity .NET `error/warning/note` не видається за SonarQube critical/major; остаточне зіставлення з рубрикою робить викладач. Для відтворення:

```bash
mkdir -p artifacts/analysis
dotnet build Microclimate.Ci.slnf -c Release -t:Rebuild -p:AnalysisOutputDirectory="$PWD/artifacts/analysis"
python3 deploy/summarize_analysis.py artifacts/analysis
```

Формат ErrorLog: [офіційна документація Microsoft](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/errors-warnings#errorlog). Правила й severity аналізаторів не послаблювались.

## C5. П'ять пунктів цілісності даних

БД — зона Учасника 1; API — Учасника 2. Клієнт не отримує доступ до БД і не приписує собі їхню реалізацію.

| Пункт | Реалізація / доказ |
|---|---|
| ≥3НФ | `docs/db-schema.md`: окремі Rooms, Sensors, Measurements, Thresholds, Alerts, Users; кімната виміру визначається через датчик |
| PK/FK/обмеження | `Infrastructure/Persistence/Configurations/EntityConfigurations.cs`: ключі, FK, UNIQUE/CHECK; міграції SQLite/PostgreSQL |
| Індекси | Індекси метрики/часу/датчика, стану тривоги, імен/ключів; див. db-schema та міграції |
| Параметризовані запити | Репозиторії Infrastructure використовують EF Core LINQ; клієнт передає DTO через HTTP, не SQL |
| Транзакції | `AppDbContext.ExecuteInTransactionAsync`; виконаний тест `ExecuteInTransaction_FailedAction_SavesNothing` перевіряє rollback |

[Знімок схеми живої SQLite](evidence/2026-11-15/sqlite-schema.md) підтверджує ключі, індекси й відсутність порушень FK. Командні інтеграційні тести запускаються в CI; `postgres-smoke` підтверджує міграції та HTTP-сценарій на справжньому PostgreSQL. Перевірка security client: токен прив'язаний до сервера, HTTPS для remote, redirect заблокований, password не зберігається.

## D1, E1–E3. Що показати та пояснити

- D1: [сценарій захисту й власні файли](participant-3-report.md). Складність пояснюється конкретно: MVVM, сесія/ролі, скасування конкурентних запитів, SignalR reconnect/fallback, графік з прогалинами, нативні платформи, CI. Експертний рівень і бал самостійно не присвоєно.
- E1: [коротка підготовка по власному коду](participant-3-defense-practice.md). Це навчальні питання, а не ненаданий офіційний банк із 400 питань. Відповідає на захисті сам учасник.
- E2: у підготовці є дві конкретні мікрозміни та готовий patch. У відокремленій копії Codex виконав репетицію: два цільові тести й регресійні сценарії пройшли, 36/36. Самостійне виконання учасником на захисті ще попереду.
- E3: README виправлено для Xcode 27.2 / SDK 10.0.404, додано точну локальну адресу, демо-вхід і отримання тестових даних. [Повторено restore/build/test і запуск API](evidence/2026-11-15/fresh-start.md) в чистому клоні з уже встановленим SDK/NuGet cache. Це не замінює перевірку сторонньою людиною в заданий викладачем час.

## Що заважає стверджувати «100% за рубрикою»

Непідтверджені дані A1/A3 та частина дедлайнів B2; прогалини щотижневої історії A2; відсутні три завершені цикли B3; очікуються людське review/merge #77; командний C3 нижче 80%; E1/E2 перевіряються особисто на захисті. Ці умови не можна закрити текстом у звіті або згенерованим коментарем.
