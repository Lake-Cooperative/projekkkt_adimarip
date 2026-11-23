# Передача на сборку и приёмку

Актуально для выпуска №70 (20.11.2026).

## Команды проверки

```bash
# 1. Статические проверки и модульные тесты (как в CI)
dotnet format whitespace backend --folder --verify-no-changes
dotnet build backend/Adimarip.sln -warnaserror
dotnet test backend/Adimarip.sln
(cd frontend && npm ci && npm run lint -- --max-warnings 0 && npm run format:check && npm run build)

# 2. Стенд и сквозные проверки
docker compose -f docker_compose.yml up -d --build
curl -f http://localhost:8081/api/ready
python3 tests/api_acceptance.py            # создаёт тестовые записи в БД
docker compose -f docker_compose.yml exec -T db sh -c 'psql -U "$POSTGRES_USER" -d "$POSTGRES_DB"' < tests/sql/schema-verification.sql
```

## Контроль корректности

1. В `__EFMigrationsHistory` есть все 6 миграций, последняя — `202611190006_RolesAndBroadcasts`; SQL-проверки не
   возвращают строк в разделах «нарушений» (пользователи без роли, незавершённые уведомления в завершённых рассылках).
2. Запреты API (401/403/404) по [`ACCEPTANCE_MATRIX.md`](ACCEPTANCE_MATRIX.md), особенно доступ служащего к чужим оповещениям.
3. Атомарность истории и аудита при создании, смене статуса и подтверждении получения.
4. Списанный адресат не предлагается при создании оповещения и не попадает в рассылку.
5. Кастомная роль: создать, назначить пользователю, изменить права — новые права действуют без повторного входа.
6. Массовое оповещение: ответ 202, отчёт доходит до `Completed`/`CompletedWithErrors`, у работника без контактов —
   канал «Портал», у `fail-mailbox@…` — ошибка в отчёте; подтверждение работника отражается в отчёте.
7. Интерфейс под каждой демо-ролью и на узком экране браузера.
8. В учебной версии нет настоящих поставщиков доставки, Госуслуг и MAX-авторизации — это ожидаемо.

## Что проверено при подготовке выпуска

Сборка всех проектов backend без предупреждений; 95 модульных тестов; форматирование C#; ESLint (0 предупреждений),
Prettier, `tsc` и сборка frontend; синтаксис `tests/api_acceptance.py`. **Не выполнялись** в среде подготовки (нет
доступа к NuGet и Docker Hub): запуск в Docker, применение миграции на PostgreSQL, сквозной сценарий — их выполняет CI.
