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

Обидва процеси отримують `Database__Provider=Postgres` і `Database__ConnectionString` з host `db`. Зараз Infrastructure ще не містить реалізації EF Core, API має службові endpoints, simulator — шаблонний Worker. Передані змінні готують майбутнє підключення; вони не доводять запис вимірів, застосування міграцій або роботу аналітики. Схему й міграції додає Учасник 1, серверні сценарії — Учасники 1 і 2.

Зупинка через `down` зберігає volume. Пароль локальний і не потрапляє у Git; не використовуйте цей development compose для публічного сервера.
