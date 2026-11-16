# Відомі обмеження

Стан на 15.11.2026. Очищення #80 накладено поверх [PR #82](https://github.com/katyadn/mppSTEM/pull/82) Учасника 1; його зміни збережено.

## Виправлення за рев'ю в #82

| Місце | Спостереження | Доказ і виправлення |
|---|---|---|
| SensorSimulator, HTTP redirects | 307 на інший origin переносить `X-Device-Key` і тіло запиту | [Рев'ю #71](https://github.com/katyadn/mppSTEM/pull/71#pullrequestreview-5912723765); спільна DI-реєстрація вимикає redirect; додано loopback-тести 307/308 |
| Application, індекс комфорту | Noise/Light з вагою 0 відкидаються до перевірки критичності; T=22 і Noise=80 дають Comfortable | [Рев'ю #50](https://github.com/katyadn/mppSTEM/pull/50#pullrequestreview-5912723871); Noise/Light тепер впливають на категорію, зберігаючи нульову вагу |
| Infrastructure, `CK_Thresholds_Order` | Nullable проміжні межі дозволяють БД прийняти CriticalMin=30, WarningMin=null, WarningMax=25, CriticalMax=null; домен відхиляє прочитані межі | [Рев'ю #51](https://github.com/katyadn/mppSTEM/pull/51#pullrequestreview-5912723972); додано перевірку всіх пар і нові міграції SQLite/PostgreSQL |

Колонка спостережень описує дефекти до #82. Виправлення перевіряються окремими регресійними тестами; PostgreSQL запускається в CI. Для main ці зміни набувають чинності після merge #82.

## Обмеження, видимі в коді

- Auth: SHA-256 паролів без солі; refresh JWT без відкликання/одноразовості й перевірки `typ`; ingest приймає JWT будь-якої ролі. Подробиці — [security.md](security.md).
- SignalR: немає читання bearer із query для браузерного WebSocket/SSE; MAUI не підписаний на AlertResolved, стан уточнюється polling. Подробиці — [realtime.md](realtime.md).
- Наявні DTO для buildings/import/anomalies/update sensor не мають відповідних endpoints. Їх залишено для сумісності публічних Contracts; перелік реалізованих маршрутів — [api.md](api.md).

Перевірені виправлення вище не усувають решти обмежень авторизації й транспорту. Стан платформ і навчальних критеріїв наведено у [verification.md](verification.md) та [рубриці](participant-3-rubric.md).
