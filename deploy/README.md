# Локальний запуск із PostgreSQL

Конфігурація відповідає `docs/db-schema.md`, розділ 6, та issue #34. Використано [офіційний PostgreSQL image](https://hub.docker.com/_/postgres) 17-alpine із volume, healthcheck і портом тільки на localhost.

```bash
cp deploy/.env.example deploy/.env
# Задайте власний POSTGRES_PASSWORD у deploy/.env.
docker compose --env-file deploy/.env -f deploy/docker-compose.yml config --quiet
docker compose --env-file deploy/.env -f deploy/docker-compose.yml up -d db
```

API і simulator запускаються окремим профілем:

```bash
docker compose --env-file deploy/.env -f deploy/docker-compose.yml --profile app up -d --build
docker compose --env-file deploy/.env -f deploy/docker-compose.yml ps
curl http://localhost:5195/health
curl http://localhost:5195/api/v1/ping
docker compose --env-file deploy/.env -f deploy/docker-compose.yml --profile app down
```

API отримує `Database__Provider=Postgres` і `Database__ConnectionString` з host `db`. API використовує реалізовані Учасниками 1 і 2 EF Core, міграції, репозиторії та предметні endpoints. Під час старту API застосовує міграції; у Development додає демо-користувачів, кімнату й датчик до порожньої БД. Симулятор надсилає виміри в API за адресою `http://api:8080/`; перед його запуском задайте в `deploy/.env` `SIMULATOR_SENSOR_ID` (id датчика з `GET /api/v1/rooms/{roomId}/sensors`) і `SIMULATOR_DEVICE_KEY`. Без них симулятор зупиняється під час старту з повідомленням, яке налаштування відсутнє. Докладніше — docs/simulator.md.

Демонстраційні користувачі й development-конфігурація JWT призначені лише для локальної розробки. Автоматичний клієнтський інтеграційний тест використовує SQLite; PostgreSQL перевіряє окремий CI job `postgres-smoke` зі справжнім Compose.

Зупинка через `down` зберігає volume. Пароль локальний і не потрапляє у Git; не використовуйте цей development compose для публічного сервера.

## Перевірка локального стека

Після старту API: `python3 deploy/smoke.py http://127.0.0.1:5195`.
Скрипт призначений для ізольованої Development-БД з demo seed: входить тестовим адміністратором, читає кімнату/датчик, зберігає температурні пороги, додає синтетичні 22.5 °C і 1900 ppm та підтверджує тривогу. Не запускайте його проти даних, які потрібно зберегти без змін. Токени не друкуються. CI створює власний stack і прибирає його disposable volume.
