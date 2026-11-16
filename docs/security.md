# Автентифікація та доступ

Опис поточної реалізації. Матриця прав на маршрути — [api.md](api.md).

## JWT і користувачі

`AuthController` перевіряє email/пароль і викликає `JwtTokenService`. Access token підписано HMAC-SHA256, TTL — 900 секунд, claims — `sub`, email, роль і `jti`. Middleware перевіряє підпис, issuer, audience та строк дії. Ролі: Admin, Operator, Viewer. `jti` сам по собі не запобігає повторному використанню токена.

Refresh token — підписаний JWT зі строком дії 7 днів. `/auth/refresh` перевіряє підпис, issuer і строк, знаходить користувача та видає нову пару. У БД немає refresh-сесій, відкликання або виявлення повторного використання. Validator не перевіряє claim `typ=refresh` і audience, тому не забезпечує належного розмежування access/refresh токенів.

Паролі зберігаються як SHA-256 без солі й адаптивної складності. Це обмеження реалізації; PasswordHasher/PBKDF2 не підключено. Створює користувачів Admin через `/users`; публічної реєстрації й серверного logout немає.

## Ключі датчиків

`SensorsController` створює випадковий 32-байтовий ключ і повертає його при створенні/ротації. Зберігається SHA-256-хеш. `DeviceKeyAuthHandler` шукає датчик за хешем заголовка `X-Device-Key`, додає claims `sensor_id`, `room_id`, роль Device та оновлює last-seen. Сіль і `FixedTimeEquals` тут не використовуються.

Приймання вимірів звіряє датчик у ключі з `sensorId` пакета; ingestion перевіряє його активність. Ротація замінює хеш, тому старий ключ більше не знаходить датчик. Endpoint також приймає JWT без обмеження за роллю, включно з Viewer.

## Клієнт і конфігурація

- MAUI зберігає access token, expiry, сервер і профіль у SecureStorage; якщо воно недоступне — лише в пам'яті з повідомленням. Пароль і refresh token не зберігаються.
- 401, закінчення TTL, logout або зміна сервера очищають сесію. 403 залишає її чинною. Вихід локальний; серверний токен діє до expiry.
- `ClientTransport` забороняє redirects; `ClientRealtime` перевіряє адресу negotiation. Remote API потребує HTTPS; HTTP дозволений для loopback і Android emulator `10.0.2.2`.
- `Jwt:SecretKey` задається конфігурацією. Публічні Development-паролі й ключі призначені для демо; власні значення передаються через user-secrets/змінні середовища. Запуск: [README](../README.md), [Compose](../deploy/README.md).

EF Core використовує параметризовані LINQ-запити. ProblemDetails не замінює перевірку прав і валідацію. Для SignalR діють [обмеження транспорту](realtime.md). Знайдені дефекти: [known-limitations.md](known-limitations.md); перевірки: [verification.md](verification.md).
