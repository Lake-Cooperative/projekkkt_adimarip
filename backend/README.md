# Backend АДИМАРИП (ASP.NET Core 8)

Слои и правило зависимостей — в [`docs/ARCHITECTURE.md`](../docs/ARCHITECTURE.md).

```bash
dotnet build backend/Adimarip.sln
dotnet test backend/Adimarip.sln
dotnet run --project backend/Adimarip.Api/Adimarip.Api.csproj --launch-profile Adimarip.Api
```

## Конфигурация

| Ключ | Назначение |
|---|---|
| `ConnectionStrings__Default` | строка подключения PostgreSQL (обязательно) |
| `Jwt__Key` | секрет подписи JWT, не короче 32 байт (обязательно) |
| `Demo__Enabled`, `Demo__Password` | демо-данные и имитация доставки |
| `Bootstrap__Username`, `Bootstrap__Password` | первый инженер автоматизации для prod-профиля |
| `Delivery__MaxParallelism` (16), `Delivery__BatchSize` (100), `Delivery__Consumers` (2), `Delivery__MaxAttempts` (3), `Delivery__RetryBaseDelayMs` (50) | массовая доставка |
| `Delivery__Simulation__LatencyMs` (15), `Delivery__Simulation__TransientFailureRate` (0) | параметры имитации каналов |
| `Cors__AllowedOrigins__N` | разрешённые источники для локальной разработки |

## Миграции

Миграции версионные и пишутся вручную (`Infrastructure/Persistence/Migrations`), снимок модели — заглушка
(см. [`docs/DATABASE_MIGRATIONS.md`](../docs/DATABASE_MIGRATIONS.md)). При старте API применяются только недостающие версии.
