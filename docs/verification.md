# Перевірки проєкту

Стан очищення #80, база `a01503a` (PR #82), 15.11.2026. Історичні UI-звіти й coverage нижче мають власні версії; їхні цифри не є поточним результатом кожного нового commit.

## Локальні результати #80

| Перевірка | Результат |
|---|---|
| Release `Microclimate.Ci.slnf`, SDK 10.0.402 | 0 warnings / 0 errors |
| Автотести | 187 passed, 0 failed, 0 skipped |
| MacCatalyst arm64, Debug | 0 warnings / 0 errors |
| iOS Simulator arm64, Debug | 0 warnings / 0 errors |
| HTTP smoke, окрема SQLite | пройдено від health/login до acknowledge |
| Таблиця REST API проти живого OpenAPI | усі 29 операцій збігаються |
| Навчальний patch в окремій копії | застосовується; 36/36 цільових і регресійних тестів |

Розподіл тестів: Client 67, Api 43, Domain 40, Application 10, Infrastructure 17, SensorSimulator 10. Нативні збірки виконані з SDK/workload set 10.0.404, Xcode 27.2, ad-hoc підписом.

У 187 збережених C#-файлах порівняно Roslyn-токени й директиви з базою: відмінності обмежені trivia, порядком using та необов'язковими кінцевими комами форматера. Видалено дві невикористані реалізації з Api/Services; реєстрація аналітики лишилась у Application. Очищення не змінює міграції з #82, XAML або поведінку публічних DTO.

## Відтворення

```bash
dotnet tool restore
git ls-files -z -- '*.cs' ':!:**/Migrations/**' | xargs -0 dotnet csharpier check
dotnet build Microclimate.Ci.slnf -c Release
dotnet test Microclimate.Ci.slnf -c Release --no-build --collect:"XPlat Code Coverage" --logger trx
```

Команди платформних збірок — [README](../README.md). HTTP smoke змінює тестові дані, тому виконується на окремій Development-БД: [deploy/README](../deploy/README.md).

## Повторна перевірка #82

[Повторне рев'ю #82](https://github.com/katyadn/mppSTEM/pull/82#pullrequestreview-5912902738): виправлення Учасника 1 перевірено окремо на `a01503a`: заборона redirects через ту саму DI-реєстрацію, категорія комфорту з Noise/Light та CHECK для nullable-меж. Додано 17 тестових випадків; загалом 187/187. SQL міграцій SQLite/PostgreSQL зберігає всі шість попарних умов; старі міграції не переписано. Очищення поверх #82 повторно звірене за токенами; його власний diff не змінює цю логіку.

## CI та докази

[CI базового main a2d6321](https://github.com/katyadn/mppSTEM/actions/runs/43757818803) пройшов усі чотири jobs. Для поточної гілки відкривати Checks її PR: `build-test`, Windows `maui-build`, `android-build`, `postgres-smoke`. Артефакти: `test-results` (TRX), `coverage-report` (HTML/Markdown), `static-analysis` (SARIF/журнал), `postgres-smoke-log`.

[Coverage 26d3fa1](https://github.com/katyadn/mppSTEM/actions/runs/43654939908) до cleanup: Client.Core 83.4% рядків / 65.8% гілок; сумарно 7 збірок — 71.8% / 53.6%. MAUI UI не входить; згенеровані JSON-метадані й міграції входять. Після видалення невикористаного коду та форматування кількість рядків може змінитися; актуальний відсоток брати з нового artifact. Кількість тестів не є відсотком покриття.

- Нативні UI-прогони до cleanup: [Mac](ui/mac-smoke-2026-11-15.md), [iOS 27.2 Simulator](ui/ios-smoke-2026-11-15.md). Очищення не змінювало UI-поведінку; новий ручний прогін не заявляється.
- Windows/Android підтверджені CI-збірками; UI цих платформ і фізичний iPhone не перевірені.
- Історичні докази: [чистий клон](evidence/2026-11-15/fresh-start.md), [статичний аналіз](evidence/2026-11-15/static-analysis.md), [схема SQLite](evidence/2026-11-15/sqlite-schema.md).
- [Відомі дефекти](known-limitations.md) та [незакриті критерії захисту](participant-3-rubric.md) зберігаються окремо.
