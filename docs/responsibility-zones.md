# Зони відповідальності

| Учасник | Каталоги | Робота |
|---|---|---|
| 1 | Domain, Application, Infrastructure, SensorSimulator | Предметні моделі, EF Core, прикладні сценарії, аналітика та симуляція |
| 2 | Api, Contracts | HTTP API, DTO, JWT, SignalR, OpenTelemetry |
| 3 | Client.Maui, Client.Core, deploy/, .github/, кореневі файли solution та документація | UI, MVVM, навігація, CI, deployment на наступних етапах та правила репозиторію |

На Етапі 0 Учасник 3 створив стандартні порожні проєкти чужих зон. У поточному main Учасники 1 і 2 реалізували Domain, Application, Infrastructure та предметний API; WeatherForecast видалено. Worker поки лише пише час у лог. Єдина зміна Worker — регістр `{Time}`, щоб відповідати аналізатору CA1727.

Клієнт виконує HTTP-виклики за DTO Учасника 2; Client.Core містить HTTP, сесію, ViewModels та проєкції без залежності від Domain. Предметні серверні endpoints реалізовано Учасником 2; при інтеграції 2026-10-24 їх збережено без змін. Legacy поля будівлі і зайві endpoints Contracts очікують узгодження Учасником 2 (issue #33). Межі зон змінює команда через зафіксоване погодження.
