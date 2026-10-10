# EF Core domain models — АДИМАРИП

Пакет содержит 10 сущностей, конфигурацию `AdimaripDbContext` и первоначальную миграцию PostgreSQL.

## Сущности
`Recipient`, `Address`, `Office`, `Notice`, `NoticeStatusHistory`, `AuditEvent`, `Notification`, `DeliveryAttempt`, `Appeal`, `Document`.

## Основные связи
- `Recipient` 1:1 `Address`
- `Recipient` 1:N `Notice`
- `Office` 1:N `Notice`
- `Notice` 1:N `NoticeStatusHistory`, `AuditEvent`, `Notification`, `Appeal`, `Document`
- `Notification` 1:N `DeliveryAttempt`
