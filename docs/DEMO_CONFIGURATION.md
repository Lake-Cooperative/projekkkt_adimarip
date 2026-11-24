# Конфигурация демонстрационного стенда

**Запуск без `.env` допускается только для локальной учебной демонстрации.**
В `docker_compose.yml` указаны публичные тестовые значения паролей БД, аккаунтов
и JWT-секрета. Ни один из этих секретов нельзя использовать на внешнем сервере.

Рекомендуемое содержимое локального `.env` (файл не коммитить):

```env
POSTGRES_DB=adimarip
POSTGRES_USER=adimarip
POSTGRES_PASSWORD=use_a_long_random_database_password
JWT_SECRET=use_at_least_32_random_bytes_generated_once
DEMO_ENABLED=true
DEMO_PASSWORD=another_unique_long_password
ASPNETCORE_ENVIRONMENT=Development
```

Демо-данные (`Infrastructure/Startup/DemoDataTask.cs`) содержат вымышленные ФИО, телефоны и адреса:
два подразделения и восемь работников для показа массового оповещения (у одного нет контактов — сработает
портал, у одного «сбойный» e-mail `fail-mailbox@…` — будет видна ошибка в отчёте). Пять учебных аккаунтов:
`operator`, `manager`, `observer`, `engineer` и `duty` (кастомная роль «Дежурный по безопасности»); пароль
равен `DEMO_PASSWORD`. Пароли хранятся в PostgreSQL в виде PBKDF2-хешей. Повторный запуск ничего не дублирует.

Параметры имитации доставки: `DELIVERY_PARALLELISM`, `DELIVERY_LATENCY_MS`, `DELIVERY_FAILURE_RATE`
(доля временных сбоев 0–1; например, `0.3` наглядно покажет повторы попыток).

Перед размещением в интернете: установите `DEMO_ENABLED=false`, удалите
тестовые аккаунты и данные, замените все секреты, включите HTTPS,
ограничьте CORS, пересмотрите разграничение прав и проведите аудит безопасности.
