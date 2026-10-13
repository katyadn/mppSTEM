# Підсумок Етапу 0 — Учасник 3

Дата початкового звіту: 2026-10-12. PR #8–#14 злиті у main за явним дорученням користувача, issues #1–#7 закриті. Командне рев’ю окремо не підтверджене. Оновлення запуску Mac: 2026-10-13.

## Issues та PR

| Крок | Issue | PR | Статус |
|---|---|---|---|
| A: Solution skeleton and build configuration | [#1](https://github.com/katyadn/mppSTEM/issues/1) | [#8](https://github.com/katyadn/mppSTEM/pull/8) | PR злитий; issue закритий |
| B: CI pipeline: build and test | [#2](https://github.com/katyadn/mppSTEM/issues/2) | [#9](https://github.com/katyadn/mppSTEM/pull/9) | PR злитий; issue закритий |
| C: Repository templates and contribution rules | [#3](https://github.com/katyadn/mppSTEM/issues/3) | [#10](https://github.com/katyadn/mppSTEM/pull/10) | PR злитий; issue закритий |
| D: README draft | [#4](https://github.com/katyadn/mppSTEM/issues/4) | [#11](https://github.com/katyadn/mppSTEM/pull/11) | PR злитий; issue закритий |
| E: MAUI client skeleton and navigation | [#5](https://github.com/katyadn/mppSTEM/issues/5) | [#12](https://github.com/katyadn/mppSTEM/pull/12) | PR злитий; issue закритий |
| F: UI wireframes | [#6](https://github.com/katyadn/mppSTEM/issues/6) | [#13](https://github.com/katyadn/mppSTEM/pull/13) | PR злитий; issue закритий |
| G: Stage 0 docs: decisions and META-USAGE | [#7](https://github.com/katyadn/mppSTEM/issues/7) | [#14](https://github.com/katyadn/mppSTEM/pull/14) | PR злитий; issue закритий |

PR злиті послідовно A→B→C→D→E→F→G через merge commits. Команди merge виконав агент за вказівкою користувача.

## Перевірки початкового Етапу 0 (2026-10-12)

- [x] .NET SDK 10.0.300 встановлено; MAUI workload 10.0.20 встановлено.
- [x] Microclimate.slnx відкрито в Rider; Rider визначив активний SDK 10.0.300 та конфігурації API http/https і SensorSimulator. Клон Rider відстежує origin/feature/7-stage0-docs.
- [x] GitHub має права push; GitHub CLI 2.102.0 встановлено та авторизовано як dimatsenko. Перші repo/API-операції виконано через наявний збережений вхід. Після окремого явного дозволу людини та її авторизації у браузері завершено офіційний OAuth-вхід CLI; `gh auth status` підтверджує успіх.
- [x] Docker Desktop 4.95.0 встановлено у `~/Applications/Docker.app`; підпис перевірено. Docker client/server 29.8.2, engine працює.
- [x] `dotnet restore Microclimate.Ci.slnf` проходить.
- [x] Команда API-запуску з README працює; шаблонний /WeatherForecast відповів HTTP 200 і п’ятьма демонстраційними записами. Після перевірки процес завершено.
- [x] Release build серверного CI-фільтра: **0 warnings, 0 errors**.
- [x] Чотири `Placeholder_ShouldPass` проходять; coverage/TRX генеруються. Це заглушки, а не тести предметної логіки.
- [x] CI PR каркасу pipeline перевірив Linux і Windows успішно: [run 38286833251](https://github.com/katyadn/mppSTEM/actions/runs/38286833251).
- [x] Швидкий старт на свіжому GitHub-клоні feature/7-stage0-docs: restore, Release build (0 warnings/0 errors) і всі чотири тести проходять. Перевірений commit bade40a; подальші правки документації та пробілів у шаблонах не змінюють реалізацію.
- [x] Фінальний MAUI-каркас (commit f6b68b7) пройшов обидва CI jobs: [run 38288647440](https://github.com/katyadn/mppSTEM/actions/runs/38288647440). Наступні гілки містять той самий перевірений код і додають документацію; актуальний статус кожного PR видно у checks.
- [ ] Ручний Windows-запуск, переходи та скриншоти.
- [ ] Командне погодження вайрфреймів і контрактів.
- [ ] Перегляд META-USAGE.md своїми словами Учасником 3.
- [ ] Налаштування захисту main адміністратором (≥1 review, required build-test).
- [x] Після merge початкових PR виконано Release build і чотири тодішні тести; подальші перевірки наведено нижче.

## Межі та відкриті блокери

- У зонах Учасників 1 і 2 лише стандартні проєкти; WeatherForecast і Worker є шаблонними прикладами. Єдина правка Worker — `{Time}` для CA1727.
- MVVM-клієнт не робить HTTP і не має DTO/справжньої автентифікації. Їх додають на Етапі 1 після погодження API.
- Dockerfile/compose належать Етапу 2; Docker engine встановлено, але контейнерний запуск не вигадано.
- macOS: 2026-10-13 блокер усунуто з .NET SDK 10.0.401, workload set 10.0.401.1 та scene lifecycle. Mac build і переходи каркасу перевірено агентом; інструкція в README. Windows UI-запуск залишається неперевіреним.
- Android SDK/JDK окремо не встановлено, Android runtime build не підтверджено; Android target присутній. Обов’язковий Windows build перевіряється на Windows runner.
- Запуск на Windows та screenshots потребують комп’ютера з Windows; зелений build не є доказом роботи UI.

## Самоперевірка перед захистом

1. **Навіщо Central Package Management?** Версії пакетів визначено один раз у Directory.Packages.props; проєкти використовують їх узгоджено, а NuGet виявляє конфлікти.
2. **Чому solution filter виключає MAUI?** Linux перевіряє .NET бібліотеки, API, worker і тести; нативний MAUI Windows збирається окремим Windows job із відповідним workload.
3. **Що таке compiled bindings?** `x:DataType` задає тип ViewModel, тому XAML-компілятор перевіряє імена властивостей і команд на збірці.
4. **Як працює ObservableProperty?** Source generator додає публічні IsBusy/Title і повідомлення зміни стану на основі полів partial-класу ObservableObject.
5. **Що дає RelayCommand?** Generator створює ICommand/async command з позначеного методу; ViewModel не має вручну писати обгортку команди.
6. **Чому Domain не має залежностей?** Предметні правила мають бути незалежні від HTTP, UI і сховища; зовнішні шари посилаються на внутрішні.
7. **Що доводять поточні тести й CI?** Запуск xUnit і успішну збірку; вони ще не доводять правильність бізнес-логіки, навігації під час виконання чи реальних датчиків.

## Після merge

На свіжому main виконати restore/build/test CI-фільтра та Windows MAUI build за README, перевірити обидва CI jobs і закриття issues. `git clean -xdf --dry-run` тільки показує можливе очищення; видалення не виконувати без окремого дозволу.


## Актуалізація 2026-10-13

Локальний main оновлено до ba1de73 з роботою Учасників 1 і 2. Виправлення Mac-запуску підготовлено від цього стану в fix/43-maccatalyst-launch, issue #43. Release build CI-фільтра: 0 warnings, 0 errors; пройшли 49 тестів (Domain 40, Application 3, API 5, Client 1). Client test залишається заглушкою.

Mac Debug rebuild: 0 errors, 2 CA1711 warnings щодо AppDelegate/SceneDelegate. Агент виконав UI-перевірку: вхід → огляд → деталі → назад; сповіщення; налаштування → вихід → повторний вхід. Застосунок залишено відкритим. Перевірка Mac не замінює Windows-запуск і командне рев’ю.
