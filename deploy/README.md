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

Обидва процеси отримують `Database__Provider=Postgres` і `Database__ConnectionString` з host `db`. API використовує реалізовані Учасниками 1 і 2 EF Core, міграції, репозиторії та предметні endpoints. Під час старту API застосовує міграції; у Development додає демо-користувачів, кімнату й датчик до порожньої БД. Simulator поки залишається шаблонним Worker і не генерує вимірів.

Демонстраційні користувачі й development-конфігурація JWT призначені лише для локальної розробки. Автоматичний клієнтський інтеграційний тест використовує SQLite; PostgreSQL слід перевіряти окремим прогоном Compose.

Зупинка через `down` зберігає volume. Пароль локальний і не потрапляє у Git; не використовуйте цей development compose для публічного сервера.
