# Microclimate — моніторинг мікроклімату приміщень

![CI](https://github.com/katyadn/mppSTEM/actions/workflows/ci.yml/badge.svg)

Навчальна .NET-система моніторингу температури, вологості, CO₂, PM2.5, шуму й освітленості. MAUI-клієнт показує кімнати, графіки та сповіщення; Operator/Admin можуть редагувати пороги. API зберігає дані через EF Core у SQLite або PostgreSQL.

| Частина | Призначення | Учасник |
|---|---|---|
| Domain, Application, Infrastructure, SensorSimulator | Модель, аналітика, БД, симулятор | 1 |
| Api, Contracts | REST, JWT/DeviceKey, SignalR, DTO | 2 |
| Client.Core, Client.Maui, deploy, .github | Клієнт, CI, запуск | 3 |

Client.Core тестується без MAUI; нативні екрани, сховище й графік — у Client.Maui. Розрахунки виконує сервер. [Зони відповідальності](docs/responsibility-zones.md), [API](docs/api.md), [перевірки](docs/verification.md), [відомі обмеження](docs/known-limitations.md).

## Вимоги

- .NET SDK **10.0.300**, зафіксований у `global.json`; `latestFeature` дозволяє сумісну новішу feature-band .NET 10.
- Для сервера й тестів MAUI workload не потрібний.
- Для MAUI: `dotnet workload install maui` та SDK потрібної платформи. Android потребує JDK та Android SDK; MacCatalyst/iOS — сумісного Xcode; Windows — Windows SDK.
- JetBrains Rider з підтримкою .NET 10, Visual Studio 2026 або VS Code + C# Dev Kit; для MAUI у VS Code також .NET MAUI extension.
- Docker Desktop потрібний для локального PostgreSQL і контейнерного запуску; див. deploy/README.md.
- Git; GitHub CLI для issues, PR та перевірки CI.

## Швидкий старт: сервер і тести (будь-яка ОС)

```bash
git clone https://github.com/katyadn/mppSTEM.git
cd mppSTEM
dotnet restore Microclimate.Ci.slnf
dotnet build Microclimate.Ci.slnf -c Release --no-restore
dotnet test Microclimate.Ci.slnf -c Release --no-build
```

У Rider відкрийте `Microclimate.slnx`. Для перевірки серверної частини з термінала використовуйте `Microclimate.Ci.slnf`; відкриття повного solution може вимагати локальних платформних SDK MAUI.

Локальний API:

```bash
dotnet run --project src/Microclimate.Api --launch-profile http
```

Порт задається в `src/Microclimate.Api/Properties/launchSettings.json`. Профіль `http` використовує SQLite, застосовує міграції й додає демо-дані до порожньої БД у Development. Перевірка готовності: `http://localhost:5195/health`, Swagger UI: `http://localhost:5195/swagger`.

Для входу в клієнт на цьому ж комп'ютері відкрийте «Адреса API та налаштування», задайте `http://localhost:5195/` і збережіть. Демо-вхід: `operator@microclimate.local` / `OperatorPassword123!`; адміністратор: `admin@microclimate.local` / `AdminPassword123!`. Ці публічні тестові значення походять із Development seed і не є обліковими даними для публічного сервера. Для Android Emulator адреса API — `http://10.0.2.2:5195/`; для iOS Simulator — `http://localhost:5195/`.

У новій БД вимірів ще немає: це очікуваний стан «Немає даних». Для демонстрації в окремій Development-БД можна виконати `python3 deploy/smoke.py http://localhost:5195`: скрипт додає синтетичні показники й змінює тестові пороги. Подробиці змін — у [deploy/README.md](deploy/README.md).

Симулятор датчиків (у другому терміналі, поки працює API). Id датчика видно в Swagger: `GET /api/v1/rooms/{roomId}/sensors`; ключ демо-датчика з Development seed — `test-device-key-12345`. У Swagger спочатку виконайте `POST /api/v1/auth/login` і передайте отриманий access token через Authorize.

```bash
dotnet user-secrets set "Simulator:Sensors:0:SensorId" "<id датчика>" --project src/Microclimate.SensorSimulator
dotnet user-secrets set "Simulator:Sensors:0:DeviceKey" "<ключ датчика>" --project src/Microclimate.SensorSimulator
dotnet run --project src/Microclimate.SensorSimulator
```

Сценарій перегріву: `Simulator:Sensors:0:Scenario` = `Overheat`. Налаштування й формули — [docs/simulator.md](docs/simulator.md).

## MAUI: Windows

У терміналі на **Windows**:

```powershell
dotnet workload install maui-windows
dotnet build src/Microclimate.Client.Maui -c Release -f net10.0-windows10.0.19041.0 -p:MauiTargetFramework=net10.0-windows10.0.19041.0
dotnet build src/Microclimate.Client.Maui -t:Run -f net10.0-windows10.0.19041.0 -p:MauiTargetFramework=net10.0-windows10.0.19041.0
```

Явне `MauiTargetFramework` обмежує restore платформою Windows, тому команда не потребує Android SDK. Перевірте Login → Dashboard, деталі кімнати, вкладки Alerts/Settings і вихід; додайте реальні скриншоти у `docs/ui/screenshots/stage-0/`.

## MAUI: macOS / iOS / Android

Перевірена поточна комбінація для Mac: **Xcode 27.2**, **.NET SDK/workload set 10.0.404**, MAUI **10.0.110**. Встановіть SDK 10.0.404 і перевірте `dotnet --version` перед командами нижче. Сумісність Apple SDK — в [офіційному релізі](https://github.com/dotnet/macios/releases/tag/dotnet-10.0.1xx-xcode27.2.10000). Мінімальна версія MacCatalyst — 17.0.

```bash
dotnet workload install maui-maccatalyst --version 10.0.404
dotnet build src/Microclimate.Client.Maui -c Debug -f net10.0-maccatalyst -p:MauiTargetFramework=net10.0-maccatalyst -p:RuntimeIdentifier=maccatalyst-arm64 -p:EnableCodeSigning=true -p:CodesignKey=-
open src/Microclimate.Client.Maui/bin/Debug/net10.0-maccatalyst/maccatalyst-arm64/Мікроклімат.app
```

`CodesignKey=-` — локальний ad-hoc підпис Debug-запуску. Якщо SecureStorage недоступне, застосунок показує «Сесія діє до закриття застосунку»; постійне сховище потребує належного підпису. API запускається в іншому терміналі за інструкцією вище. [Нативний прогін 15.11](docs/ui/mac-smoke-2026-11-15.md) містить фактичні сценарії й скриншоти. Для старого Xcode 27.1 історична комбінація SDK 10.0.402 / workload 10.0.402.1 описана у [прогоні 24.10](docs/ui/mac-smoke-2026-10-24.md).

На Intel Mac замініть runtime на `maccatalyst-x64`.

Для iOS Simulator на Apple Silicon використовуйте ту саму комбінацію Xcode/SDK:

```bash
dotnet workload install maui-ios --version 10.0.404
dotnet build src/Microclimate.Client.Maui -c Debug -f net10.0-ios -p:MauiTargetFramework=net10.0-ios -p:RuntimeIdentifier=iossimulator-arm64 -p:EnableCodeSigning=true -p:CodesignKey=-
xcrun simctl list devices available
# Після вибору й запуску потрібного Simulator:
xcrun simctl install booted src/Microclimate.Client.Maui/bin/Debug/net10.0-ios/iossimulator-arm64/Microclimate.Client.Maui.app
xcrun simctl launch booted com.companyname.microclimate.client.maui
```

[Прогін iOS 27.2](docs/ui/ios-smoke-2026-11-15.md) підтверджує основні екрани, live update і відновлення сесії. Simulator потребував ad-hoc підпису; `EnableCodeSigning=false` спричинив системне відхилення запуску. На фізичному iPhone потрібен окремий підпис і перевірка.

Для Android встановіть .NET SDK 10.0.404, JDK 21 та Android SDK. Потім:

```bash
dotnet workload install maui-android --version 10.0.404
dotnet build src/Microclimate.Client.Maui -c Release -f net10.0-android -p:MauiTargetFramework=net10.0-android
```

## Запуск через Docker

[Інструкція Docker Compose](deploy/README.md): PostgreSQL запускається окремо; профіль app збирає API й simulator та передає конфігурацію Database. API застосовує міграції PostgreSQL під час старту; симулятор надсилає виміри від датчика, заданого в deploy/.env.

## Документація

- [Правила внеску](CONTRIBUTING.md)
- [Архітектурні рішення](DECISIONS.md)
- [Журнал генерації та ШІ](META-USAGE.md)
- [Перевірки й coverage](docs/verification.md)
- [Стан клієнта](docs/client-progress.md) і [підготовка до захисту](docs/participant-3-report.md)
- [Підсумковий чекліст](docs/stage-0-checklist.md)
