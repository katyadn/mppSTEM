# REST API v1

Опис реалізації `src/Microclimate.Api/Controllers/V1` і DTO у `Microclimate.Contracts`. Базовий шлях — `/api/v1`. Swagger (`/swagger`) та OpenAPI (`/openapi/v1.json`) доступні в Development. Автентифікація: [security.md](security.md); події: [realtime.md](realtime.md).

## Формат даних

- JSON: camelCase; enum у тілі й відповідях — числа. `MetricType`: Temperature=1, Humidity=2, Co2=3, Pm25=4, Noise=5, Light=6. У query/path можна передавати імена, наприклад `metric=co2`.
- Час — ISO 8601 з UTC/offset; домен нормалізує його до UTC. Ідентифікатори у DTO — UUID.
- Пагінація: `page` від 1, `pageSize` від 1 до 100, типово 20. `PagedResponse<T>` містить `items`, `page`, `pageSize`, `totalCount`.
- **Усі ролі** = Viewer, Operator, Admin; **оператор** = Operator або Admin; **адмін** = Admin.

## Маршрути

Шляхи відносні до `/api/v1`. Назви типів відповідають Contracts.

| Метод | Шлях | Доступ | Успішна відповідь |
|---|---|---|---|
| GET | `/ping` | анонімний | 200 `PingResponse` |
| POST | `/auth/login` | анонімний | 200 `TokenResponse` |
| POST | `/auth/refresh` | анонімний, потрібен токен у тілі | 200 `TokenResponse` |
| GET | `/auth/me` | JWT | 200 `UserResponse` |
| GET | `/rooms` | усі ролі | 200 `PagedResponse<RoomResponse>` |
| GET | `/rooms/{id}` | усі ролі | 200 `RoomResponse` |
| POST | `/rooms` | адмін | 201 `RoomResponse` |
| PUT | `/rooms/{id}` | адмін | 200 `RoomResponse` |
| DELETE | `/rooms/{id}` | адмін | 204 |
| GET | `/rooms/{roomId}/sensors` | усі ролі | 200 `SensorResponse[]` |
| GET | `/sensors/{id}` | усі ролі | 200 `SensorResponse` |
| POST | `/sensors` | адмін | 201 `SensorCreatedResponse` із ключем |
| DELETE | `/sensors/{id}` | адмін | 204, датчик деактивовано |
| POST | `/sensors/{id}/rotate-key` | адмін | 200 `SensorCreatedResponse` з новим ключем |
| POST | `/measurements` | JWT будь-якої ролі або X-Device-Key | 202 `{accepted, alertsRaised, alertsResolved}` |
| GET | `/rooms/{roomId}/measurements` | усі ролі | 200 `PagedResponse<MeasurementResponse>` |
| GET | `/rooms/{roomId}/measurements/latest` | усі ролі | 200 `LatestMetricResponse[]` |
| GET | `/rooms/{roomId}/measurements/export` | оператор | 200, файл CSV/JSON |
| GET | `/rooms/{roomId}/analytics/summary` | усі ролі | 200 `MetricSummaryResponse[]` |
| GET | `/rooms/{roomId}/analytics/series` | усі ролі | 200 `SeriesPointResponse[]` |
| GET | `/rooms/{roomId}/analytics/comfort` | усі ролі | 200 `ComfortIndexResponse` |
| GET | `/rooms/{roomId}/thresholds` | усі ролі | 200 `ThresholdResponse[]` |
| PUT | `/rooms/{roomId}/thresholds/{metric}` | оператор | 200 `ThresholdResponse` |
| GET | `/alerts` | усі ролі | 200 `PagedResponse<AlertResponse>` |
| POST | `/alerts/{id}/acknowledge` | оператор | 200 `AlertResponse` |
| GET | `/users` | адмін | 200 `UserResponse[]` |
| POST | `/users` | адмін | 201 `UserResponse` |
| PUT | `/users/{id}/role` | адмін | 200 `UserResponse` |
| DELETE | `/users/{id}` | адмін | 204, користувача видалено |

`GET /health` поза `/api/v1` повертає `Healthy`. Реєстрації, серверного logout, buildings, імпорту вимірів, anomalies та PUT sensor зараз немає. Залишкові DTO для цих сценаріїв не означають наявності маршрутів.

## Параметри та правила

| Запит | Параметри / поведінка |
|---|---|
| Кімнати | `page`, `pageSize`; фільтра за будівлею немає |
| Історія вимірів | `metric`, `from`, `to`, `page`, `pageSize`; типовий період — останні 7 днів |
| Експорт | необов'язковий `metric`, `from`, `to`, `format=csv/json`; типово останній місяць і CSV; відповідь потоком з `Content-Disposition` |
| Summary | необов'язковий `metric`, `from`, `to`; типово 7 днів, максимум 90 |
| Series | `metric`, `from`, `to`, `interval=hour/day`; типово доба й hour; максимум 31 день для hour, 90 для day |
| Comfort | останні 15 хвилин; параметрів періоду немає |
| Alerts | необов'язкові `roomId`, `level`, `status`, а також `page`, `pageSize` |
| Thresholds | ефективні межі кімнати або стандартні з `MetricCatalog`; PUT змінює одну метрику |

Пакет вимірів містить `sensorId` і непорожній `items` (до 500 записів):

```json
{"sensorId":"00000000-0000-0000-0000-000000000001","items":[{"metric":1,"value":22.5,"measuredAt":"2026-11-15T12:00:00Z"}]}
```

Значення мають бути скінченними й у фізичному діапазоні `MetricCatalog`; час — не пізніше ніж на 5 хвилин у майбутньому. Для DeviceKey `sensorId` має збігатися з датчиком ключа. Повторення метрики в пакеті дозволене; для перевірки тривог береться найновіший її вимір. Порожній/завеликий пакет повертає 400.

## Помилки й перевірки

`GlobalExceptionHandler` перетворює NotFound → 404, Conflict → 409, ApplicationValidation/DomainException → 400, інші винятки → 500. Його ProblemDetails містить `type`, `title`, `status`, `instance`, `traceId`, а валідація — також `errors`. Деталі внутрішнього винятку приховані поза Development. MVC-валідація та 401/403 мають окремі обробники, тому додаткові поля можуть відрізнятися.

HTTP-контракти перевіряються в `tests/Microclimate.Api.IntegrationTests`; запуск і результати — [verification.md](verification.md). Відомі розбіжності бізнес-правил — [known-limitations.md](known-limitations.md).
