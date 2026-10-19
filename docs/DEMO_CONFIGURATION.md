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

Данные `DemoSeed` содержат вымышленные ФИО, телефоны и адреса. Четыре
учебных аккаунта: `operator`, `manager`, `observer`, `engineer`; пароль
равен `DEMO_PASSWORD`. Пароли хранятся в PostgreSQL в виде PBKDF2-хешей.

Перед размещением в интернете: установите `DEMO_ENABLED=false`, удалите
тестовые аккаунты и данные, замените все секреты, включите HTTPS,
ограничьте CORS, пересмотрите разграничение прав и проведите аудит безопасности.
