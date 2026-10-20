# Реестр оповещений «АДИМАРИП»

Учебный проект [кооператива «Озеро»](https://github.com/Lake-Cooperative). **Текущее состояние: чекпоинт 3 — первая рабочая версия.** Используются тестовые данные.

## Реализовано сейчас

- ASP.NET Core 8 API с `GET /api/health`, возвращающим HTTP 200 и JSON `{"status":"ok","service":"adimarip-api"}`.
- Swagger/OpenAPI (`/swagger` при `ASPNETCORE_ENVIRONMENT=Development`).
- React 18 + TypeScript + Vite: оформленная главная страница и проверка состояния API.
- Docker Compose: UI, API и PostgreSQL 16 (БД пока не содержит автоматически созданных таблиц).
- Существующая библиотека EF Core 8 / Npgsql подключена к API **на уровне проекта и DI**. Миграции **не выполняются автоматически** (ограничение).
- Актуализированная документация проекта и [план этапов](docs/ROADMAP.md).
- JWT, ролевой доступ, CRUD-реестр, адресаты, статусы, история, обращения, документы, автоматическое заполнение БД

## Запуск всего каркаса через Docker

Нужен Docker Engine + Compose plugin и доступ к Docker Hub, NuGet и npm (для сборки образов).

```bash
# из корня проекта
cp .env.example .env
```
``` bash
# необязательно; демонстрационные значения уже в compose
docker compose -f docker_compose.yml up --build
```

| Компонент | URL |
|---|---|
| Главная страница | http://localhost:8080 |
| API | http://localhost:8081 |
| Health check | http://localhost:8081/api/health |
| Swagger (Development) | http://localhost:8081/swagger |

Проверка:

```bash
curl -i http://localhost:8081/api/health
bash tests/health-smoke.sh
```

Ожидается HTTP 200, `status: ok`. Если React показывает «API недоступен», проверьте, что backend запущен на порту `8081` и браузер открыт на `localhost:8080` (CORS разрешён для 8080 и 5173).

Остановка: `docker compose -f docker_compose.yml down` (данные volume сохраняются). Для полного удаления **тестового** тома: `docker compose -f docker_compose.yml down -v`.

## Разработка без Docker

Нужны .NET SDK 8, Node.js 20+/22, npm; при первом запуске требуется доступ к NuGet/npm.

Терминал 1:

```bash
dotnet run --project backend/Adimarip.Api/Adimarip.Api.csproj --launch-profile Adimarip.Api
```

Терминал 2:

```bash
cd frontend
npm install
npm run dev
```

В браузере `http://localhost:5173`; UI запрашивает `http://localhost:8081/api/health`. PostgreSQL для этой ручки необязателен. Локальный запуск **не применяет миграции**.

## Структура

- `backend/Adimarip.Api/` — ASP.NET Core 8 API;
- `frontend/` — React / TypeScript / Vite;
- `adimarip-efcore/Adimarip.Data/` — ранее добавленные сущности и черновая миграция;
- `docs/` — бизнес-, системные требования, Use Cases, ERD и ROADMAP;
- `tests/health-smoke.sh` — HTTP smoke test;
- `docker_compose.yml` — контейнеризация для семинара 2.

## Дальнейшее развитие и ограничения

Требования и правила ролей см. в [`business_requirements.md`](docs/business_requirements.md), [`system_requirements.md`](docs/system_requirements.md), [`UseCases_AND_Functions.md`](docs/UseCases_AND_Functions.md). ERD согласована с фактическими C#-моделями: [`erd.md`](docs/erd.md). Ход работы по контрольным точкам: [`seminar-2-status.md`](docs/seminar-2-status.md), [`ROADMAP.md`](docs/ROADMAP.md).

Исходные EF миграция и снимок модели подлежат проверке/приведению к каноническому виду перед применением к БД. **Реальной отправки** SMS/e-mail и хранения файлов не планируется (в рамках лабораторных работ); данные исключительно тестовые. Демонстрационные пароли из Compose **не годятся** для продакшна (введенное ограничение). 

## Лицензия

Лицензия была выбрана советом [кооператива «Озеро»](https://github.com/Lake-Cooperative). Основная причина - данная лицензия полностью соответствует принципам участникам команды в области разработки и открытого кода.

[LICENSE](LICENSE)
