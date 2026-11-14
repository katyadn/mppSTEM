# Клієнт Учасника 3

Оновлено 2026-11-15, issue #76. Це стан вимог клієнта з `docs/ui/wireframes.md` та інтеграції з актуальним `main`, а не відсоток тестового покриття або готовності всієї команди.

## Реалізовано

| Вимога | Реалізація |
|---|---|
| Вхід і сесія | JWT login → auth/me, відновлення, завершення TTL, 401 → вихід, 403 зберігає сесію |
| Огляд | Пагінація кімнат, останні виміри з `measurements/latest`, час виміру, категорія комфорту сервера |
| Аналітика | 6 метрик, 24 години / 7 / 30 днів, автоматичне застосування вибору, min/avg/max, осі, одиниці, місцевий час, вибір точки графіка |
| Відсутні дані | Окремий NoData, справжній 0 не губиться, пропущені інтервали розривають лінію |
| Пороги | Перегляд ефективних меж; Admin/Operator можуть змінювати; Viewer читає. Порожнє поле = відсутня межа; крапка/кома, скінченність і порядок перевіряються до HTTP |
| Сповіщення | Фільтри, пагінація, перехід у кімнату, acknowledge для відкритих записів Admin/Operator |
| SignalR | Автентифікований канал, актуальні методи SubscribeToRoom/SubscribeToAllAlerts, відновлення підписок, об'єднання подій до одного оновлення за 5 с |
| Стійкість | При втраті хаба HTTP-перевірка кожні 30 с; повторний початковий connect; завершення при logout/зміні сервера/TTL, зупинка в background |
| Налаштування | Адреса API, профіль, фактичний стан SecureStorage, система/світла/темна тема з Preferences, вихід |
| CI і deployment | Linux build/test/coverage, Windows MAUI, Android MAUI, PostgreSQL + API + HTTP smoke у Compose |

## Межі архітектури та безпека

`Client.Core` містить HTTP, сесію, SignalR, ViewModels і проєкції; `Client.Maui` — XAML, Shell, Preferences, SecureStorage, UI-dispatch і графік. Серверні Domain/Application/Infrastructure/Api/Contracts у цій задачі не змінено.

Сесія зберігає лише access token, expiry, сервер і профіль. Пароль і refresh token не записуються. Remote API потребує HTTPS; HTTP дозволений лише localhost/loopback/Android emulator 10.0.2.2. Redirect вимкнено; SignalR negotiation додатково не може перенести запит до іншого сервера. Роль у UI не замінює серверну авторизацію.

Огляд не називає старі виміри актуальним комфортом: показує час останнього виміру, а сервер може повернути «Немає актуальної оцінки комфорту». Для сторінки з 20 кімнат виконується максимум 4 паралельних ланцюжки latest/comfort. Серверні розрахунки на клієнті не дублюються. Одиниці фіксовані за Contracts (°C, %, ppm, µg/m³, dB(A), lux); конвертація в інші системи не входить до погодженої моделі.

Живі оновлення зберігають завантажені сторінки списків і чернетку порогів. Відсутня серверна подія AlertResolved компенсується періодичним читанням. Текст стану каналу показано на екрані.

## Перевірки

```bash
dotnet build Microclimate.Ci.slnf -c Release
dotnet test Microclimate.Ci.slnf -c Release --no-build
# На Mac: .NET SDK / workload set 10.0.404 та Xcode 27.2.
dotnet workload install maui-maccatalyst --version 10.0.404
dotnet build src/Microclimate.Client.Maui -c Debug -f net10.0-maccatalyst \
  -p:MauiTargetFramework=net10.0-maccatalyst -p:RuntimeIdentifier=maccatalyst-arm64
```

Локально 2026-11-15: 168/168 тестів (67 Client, 43 API, 40 Domain, 6 Application, 4 Infrastructure, 8 Simulator). Release solution та повна нативна Debug MacCatalyst збірка: 0 warnings / 0 errors. MAUI знову має `TreatWarningsAsErrors=true`; CA1711 приглушено тільки для UIKit AppDelegate/SceneDelegate з поясненням імені.

Нові тести перевіряють threshold round-trip через реальний API/SQLite, ролі й 403, відсутні/неправильні межі, живі сторінки списків, тему, автоматичний період, справжній SignalR hub через TestServer/LongPolling, підписки після reconnect, busy UI, logout, fallback і заборону redirect. `deploy/smoke.py` пройшов проти ізольованого локального API/SQLite; PostgreSQL перевіряється окремим CI job, його результат не підміняється SQLite-тестами.

Деталі нативного UI-прогону та межі перевірки — `docs/ui/mac-smoke-2026-11-15.md`. Ручні Windows/Android UI-прогони й постійне SecureStorage з належним підписом не підтверджені цим запуском. Збірка платформи не є доказом ручного UI-прогону. Merge виконує людина після рев'ю Учасника 2 та зеленого CI згідно `CONTRIBUTING.md`.
