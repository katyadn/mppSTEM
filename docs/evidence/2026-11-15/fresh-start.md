# Відтворення запуску з чистого клону

15.11.2026, commit f5e0e9e; окремий клон без bin/obj та попередньої БД. .NET SDK 10.0.402 і NuGet cache вже встановлені. Це перевірка README, а не вимір запуску на повністю новій машині або в заданий викладачем час.

1. `dotnet restore Microclimate.Ci.slnf` — успішно.
2. `dotnet build Microclimate.Ci.slnf -c Release --no-restore -p:AnalysisOutputDirectory=…` — 0 warnings / 0 errors, [13 SARIF-звітів](static-analysis.md).
3. `dotnet test Microclimate.Ci.slnf -c Release --no-build --logger trx` — **168/168** (67 Client, 43 API, 40 Domain, 6 Application, 4 Infrastructure, 8 Simulator).
4. `dotnet run --project src/Microclimate.Api --launch-profile http --no-build -c Release` — створені SQLite/міграції/demo seed, health 200 на localhost:5195.
5. `python3 deploy/smoke.py http://127.0.0.1:5195` — health, JWT, rooms, sensors, thresholds, ingestion, latest, comfort, alerts, acknowledge.

Під час повторення пункту 5 знайдено дефект самого smoke: попередня acknowledged Critical-тривога не створювалася повторно. Виправлений скрипт спочатку надсилає нормальний CO₂ 800 ppm (закриває стару тривогу), потім 1900 ppm (створює нову), перевіряє acknowledge саме її ID. Після виправлення **два послідовні запуски на одній БД пройшли**. CI повторює той самий сценарій двічі на PostgreSQL.

У журналі цього API-прогону пошук `Unhandled|fatal|fail:|crit:|exception` не дав збігів. Токени й паролі із запитів у докази не публікуються. [Фактична схема SQLite](sqlite-schema.md) містить PK/FK/індекси та порожній `foreign_key_check`; це не підміна окремого PostgreSQL CI.

Нативні сценарії: [Mac](../../ui/mac-smoke-2026-11-15.md), [iOS](../../ui/ios-smoke-2026-11-15.md). Сирі SARIF, build log, TRX і coverage публікуються CI artifacts; локальні підсумки вище є датованим знімком виконаної перевірки.
