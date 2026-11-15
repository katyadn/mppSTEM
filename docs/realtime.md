# SignalR: живі оновлення

Хаб — `/hubs/alerts`, реалізація `Microclimate.Api/Hubs/AlertsHub.cs`; публікація — `SignalRAlertNotifier`. Потрібен JWT. .NET-клієнт передає його через `AccessTokenProvider`. Сервер не налаштовує `OnMessageReceived` для `access_token` у query, тому браузерний WebSocket/SSE transport із токеном у URL наразі не підтримано.

## Підписки

| Метод хаба | Група |
|---|---|
| `SubscribeToRoom(Guid roomId)` / `UnsubscribeFromRoom(Guid roomId)` | `room-{roomId}` |
| `SubscribeToAllAlerts()` / `UnsubscribeFromAllAlerts()` | `all-alerts` |

Автоматичного вступу в групи немає; після reconnect клієнт підписується повторно. Усі методи доступні автентифікованим користувачам; додаткової перевірки ролі для `all-alerts` немає.

## Події сервера

| Подія | Поля payload | Одержувачі |
|---|---|---|
| `MeasurementReceived` | `roomId`, `metric`, `value`, `measuredAt` | кімната |
| `AlertRaised` | `alertId`, `roomId`, `roomName`, `metric`, `level`, `value`, `threshold`, `message`, `createdAt` | кімната й all-alerts |
| `AlertAcknowledged` | `id`, `roomId`, `acknowledgedAt`, `acknowledgedByUserId` | кімната й all-alerts |
| `AlertResolved` | `id`, `roomId`, `resolvedAt` | кімната й all-alerts |

Перші дві події мають DTO у `Contracts/Realtime`; acknowledged/resolved формуються анонімними об'єктами. Ingestion публікує зміни після збереження. Серверного обмеження «одна подія за 5 секунд» немає.

## Поведінка MAUI

`ClientRealtime` слухає MeasurementReceived, AlertRaised та AlertAcknowledged. Подія позначає дані як змінені; видима сторінка перечитує їх через HTTP. Клієнт об'єднує події до одного оновлення за 5 секунд і перевіряє стан кожні 30 секунд, зокрема при недоступному хабі. AlertResolved сервер надсилає, але клієнт окремо на неї не підписаний; її результат потрапляє на екран під час періодичного читання.

Reconnect відновлює підписки. `PageLiveUpdates` передає зміни у UI-потік; вихід зі сторінки/background зупиняє watcher, logout/зміна сервера/expiry — сесію каналу. Оновлення зберігає завантажені сторінки списків, вибрану точку й чернетку порогів.

`RealtimeWorkflowTests` перевіряє справжній хаб через TestServer/LongPolling, повторні підписки, polling, logout та redirect. Нативні сценарії й результати: [verification.md](verification.md).
