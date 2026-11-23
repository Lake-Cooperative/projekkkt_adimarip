# Реестр оповещений «АДИМАРИП»

Учебный проект [кооператива «Озеро»](https://github.com/Lake-Cooperative) по основам ООП. Используются только вымышленные данные;
внешняя доставка SMS/e-mail **имитируется**.

**Текущее состояние (19.11.2026):** полноценный реестр с ролями и правами, массовым оповещением работников и кастомными ролями.
Что нового — [`docs/RELEASE_70.md`](docs/RELEASE_70.md), устройство кода и паттерны — [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md),
контракт API — [`docs/API.md`](docs/API.md), план — [`docs/ROADMAP_PRERELEASE.md`](docs/ROADMAP_PRERELEASE.md),
перечень всей документации — [`docs/README.md`](docs/README.md).

## Возможности

- Вход по JWT, права проверяются на сервере; четыре системные роли и **кастомные роли** из набора прав.
- Справочники адресатов и подразделений, списание адресата с сохранением истории.
- Реестр оповещений: создание, поиск, фильтры, пагинация, карточка, смена статуса по конечному автомату, история и аудит.
- Уведомления по отдельным оповещениям, обращения, ссылки на документы.
- **Массовое оповещение работников**: все / подразделения / выбранные, важность Info–Critical, фоновая приоритетная очередь,
  параллельная доставка с повторами, резервный канал «Портал», отчёт и подтверждение получения.

## Запуск через Docker

Нужны Docker Engine + Compose plugin и доступ к Docker Hub, NuGet и npm (для сборки образов).

```bash
cp .env.example .env          # необязательно: демонстрационные значения уже заданы в compose
docker compose -f docker_compose.yml up --build
```

| Компонент | URL |
|---|---|
| Интерфейс | http://localhost:8080 |
| API | http://localhost:8081 |
| Health / Readiness | http://localhost:8081/api/health, http://localhost:8081/api/ready |
| Swagger (Development) | http://localhost:8081/swagger |

Миграции БД применяются автоматически при старте API.

### Демо-учётные записи (`DEMO_ENABLED=true`)

Пароль — значение `DEMO_PASSWORD` (по умолчанию `demo-password-change-me`).

| Логин | Роль | Что показать |
|---|---|---|
| `operator` | Секретарь | реестр, создание оповещений, статусы, массовое оповещение |
| `manager` | Управитель | всё, что у секретаря, + журнал аудита |
| `observer` | Служащий | только свои оповещения, баннер срочных, «Ознакомлен(а)» |
| `engineer` | Инженер автоматизации | учётные записи, кастомные роли |
| `duty` | Дежурный по безопасности (кастомная) | реестр и массовые оповещения без права создавать записи |

Остановка: `docker compose -f docker_compose.yml down` (данные сохраняются), полное удаление тестового тома — `down -v`.

## Разработка без Docker

Нужны .NET SDK 8, Node.js 20+, PostgreSQL 16.

```bash
# backend (строка подключения и секрет — через переменные окружения)
export ConnectionStrings__Default="Host=localhost;Database=adimarip;Username=adimarip;Password=..."
export Jwt__Key="минимум-32-байта-случайного-секрета" Demo__Enabled=true Demo__Password=demo-password-change-me
dotnet run --project backend/Adimarip.Api/Adimarip.Api.csproj --launch-profile Adimarip.Api

# frontend
cd frontend && npm install && npm run dev      # http://localhost:5173, /api проксируется на :8081
```

## Проверки

```bash
dotnet format whitespace backend --folder --verify-no-changes   # оформление C# по .editorconfig
dotnet build backend/Adimarip.sln -warnaserror                  # сборка без предупреждений
dotnet test backend/Adimarip.sln                                # модульные тесты (домен, сценарии, доставка)
cd frontend && npm ci && npm run lint && npm run format:check && npm run build   # ESLint, Prettier, типы, сборка
python3 tests/api_acceptance.py                                 # сквозной сценарий против запущенного стека (создаёт тестовые записи)
```

Исправить оформление автоматически: `dotnet format whitespace backend --folder`, `cd frontend && npm run format`.
В CI (`.github/workflows/ci.yml`) выполняются все шаги и SQL-проверки `tests/sql/schema-verification.sql`.

## Структура

```
backend/
  Adimarip.Domain/          сущности и бизнес-правила (без зависимостей)
  Adimarip.Application/     сценарии использования и интерфейсы (порты)
  Adimarip.Infrastructure/  EF Core + PostgreSQL, миграции, очередь рассылок, имитаторы каналов
  Adimarip.Api/             HTTP-эндпоинты, JWT, проверка прав
  Adimarip.Tests/           модульные тесты xUnit
frontend/src/
  api/ hooks/ components/ shared/ app/ features/{notices,broadcasts,recipients,admin}
docs/       требования, ТЗ, ERD, архитектура, roadmap, заметки к выпускам
tests/      API-приёмка, SQL-проверки
deploy/     Caddyfile для prod-профиля (HTTPS)
```

## Ограничения

Реальной отправки SMS/e-mail и хранения файлов нет — только имитация и метаданные. Демонстрационные пароли и секреты
из Compose **не годятся** для внешнего развёртывания: см. [`docs/PRODUCTION_DEPLOYMENT.md`](docs/PRODUCTION_DEPLOYMENT.md).
В prod-профиле демо-режим выключен, поэтому уведомления и массовые оповещения отвечают `501`.

## Лицензия

Лицензия выбрана советом [кооператива «Озеро»](https://github.com/Lake-Cooperative): [LICENSE](LICENSE).
