# ERD — реестр оповещений «АДИМАРИП»

**Статус:** действующая схема БД после миграции `202611190006_RolesAndBroadcasts` (20.11.2026).
Таблицы создаются и обновляются автоматически при старте API (`Database.MigrateAsync`, см.
[`DATABASE_MIGRATIONS.md`](DATABASE_MIGRATIONS.md)). Сущности — `backend/Adimarip.Domain`, отображение —
`backend/Adimarip.Infrastructure/Persistence/Configurations`.

Ключи — `int`/`bigint` (identity), у учётных записей и ролей — строковые (`Username`, `Name`). Авторы действий
(`AuthorUserId`, `ChangedByUserId`, `UserId`, `CreatedByUserId`, `ArchivedByUserId`) хранятся логином без внешнего
ключа — чтобы журналы не зависели от удаления учётных записей (открытый вопрос О-4).

```mermaid
erDiagram
    OFFICES {
        int Id PK
        varchar Name UK "200"
    }
    RECIPIENTS {
        int Id PK
        varchar ExternalIdentifier UK "100"
        varchar FullName "250"
        date DateOfBirth
        varchar Email "320, nullable"
        varchar Phone "50, nullable"
        int OfficeId FK "nullable"
    }
    ADDRESSES {
        int Id PK
        int RecipientId FK, UK
        varchar PostalCode "nullable"
        varchar Region "nullable"
        varchar City "nullable"
        varchar Street "nullable"
        varchar House "nullable"
        varchar Apartment "nullable"
    }
    RECIPIENT_ARCHIVES {
        int RecipientId PK, FK
        timestamptz ArchivedAt
        varchar ArchivedByUserId "логин"
        varchar Reason "1000"
    }
    NOTICES {
        int Id PK
        varchar Number UK "100"
        int RecipientId FK
        int OfficeId FK
        int BroadcastId FK "nullable"
        varchar AuthorUserId "логин"
        date CreatedAt
        date ProcessingDeadline
        varchar Reason "1000"
        varchar Comment "4000, nullable"
        varchar CurrentStatus "Created..Failed"
        xid xmin "системный, версия строки"
    }
    NOTICE_STATUS_HISTORIES {
        bigint Id PK
        int NoticeId FK
        varchar Status
        varchar Comment "nullable"
        timestamptz ChangedAt
        varchar ChangedByUserId "логин"
    }
    AUDIT_EVENTS {
        bigint Id PK
        int NoticeId FK
        varchar Action
        varchar UserId "логин"
        timestamptz OccurredAt
        varchar ObjectType "nullable"
        varchar ObjectId "nullable"
        varchar Details "nullable"
    }
    NOTIFICATIONS {
        int Id PK
        int NoticeId FK
        varchar Channel "Email | Sms | Portal | Max"
        varchar RecipientAddress "320"
        timestamptz CreatedAt
        varchar Status "Pending | Delivered | Failed"
        varchar LastError "500, nullable"
    }
    DELIVERY_ATTEMPTS {
        bigint Id PK
        int NotificationId FK
        varchar Status "Delivered | Failed"
        timestamptz AttemptedAt
    }
    APPEALS {
        int Id PK
        int NoticeId FK
        varchar Type "100"
        varchar Text "4000"
        varchar Status "Submitted"
        timestamptz SubmittedAt
    }
    DOCUMENTS {
        int Id PK
        int NoticeId FK
        varchar Name "255"
        varchar MimeType "150"
        varchar StorageUri "https, 2048"
        timestamptz AddedAt
    }
    APP_USERS {
        varchar Username PK "100"
        varchar DisplayName "200"
        text PasswordHash "PBKDF2"
        varchar Role FK "40"
        int RecipientId FK "nullable"
        timestamptz CreatedAt
    }
    ROLES {
        varchar Name PK "40"
        varchar DisplayName "100"
        boolean IsSystem
        timestamptz CreatedAt
    }
    ROLE_PERMISSIONS {
        varchar RoleName PK, FK
        varchar Permission PK "одно из 17 прав"
    }
    BROADCASTS {
        int Id PK
        varchar Code UK "MO-yyMMdd-XXXXXX"
        varchar Title "200"
        varchar Message "4000"
        varchar Severity "Info | Warning | Critical"
        varchar Channels "Email,Sms,..."
        int OfficeId FK "отправитель"
        varchar AudienceDescription "300"
        varchar CreatedByUserId "логин"
        timestamptz CreatedAt
        timestamptz StartedAt "nullable"
        timestamptz CompletedAt "nullable"
        varchar Status "Queued | Dispatching | Completed | CompletedWithErrors"
        int TargetCount
        int DeliveredCount
        int FailedCount
    }

    OFFICES ||--o{ RECIPIENTS : "работает в"
    RECIPIENTS ||--o| ADDRESSES : "адрес"
    RECIPIENTS ||--o| RECIPIENT_ARCHIVES : "списание"
    RECIPIENTS ||--o{ NOTICES : "получает"
    OFFICES ||--o{ NOTICES : "обрабатывает"
    BROADCASTS ||--|{ NOTICES : "оповещение каждому работнику"
    OFFICES ||--o{ BROADCASTS : "отправитель"
    NOTICES ||--|{ NOTICE_STATUS_HISTORIES : "история"
    NOTICES ||--|{ AUDIT_EVENTS : "аудит"
    NOTICES ||--o{ NOTIFICATIONS : "уведомления"
    NOTIFICATIONS ||--o{ DELIVERY_ATTEMPTS : "попытки"
    NOTICES ||--o{ APPEALS : "обращения"
    NOTICES ||--o{ DOCUMENTS : "метаданные"
    ROLES ||--|{ ROLE_PERMISSIONS : "набор прав"
    ROLES ||--o{ APP_USERS : "назначена"
    RECIPIENTS ||--o| APP_USERS : "привязка работника"
```

## Кардинальности и правила удаления

| Связь | Кратность | Ограничения |
|---|---|---|
| Office → Recipient | 1 : 0..N | `ON DELETE SET NULL` — работник остаётся без подразделения |
| Recipient → Address | 1 : 0..1 | `Address.RecipientId` уникален, каскадное удаление |
| Recipient → RecipientArchive | 1 : 0..1 | Наличие записи = адресат списан; `RESTRICT` |
| Recipient / Office → Notice | 1 : 0..N | `RESTRICT`; нельзя создать оповещение без существующих адресата и подразделения |
| Broadcast → Notice | 1 : 1..N | `RESTRICT`; номер оповещения — `{Code}-{RecipientId}` |
| Notice → StatusHistory, AuditEvent | 1 : 1..N | Пишутся одной транзакцией с изменением оповещения; через API не меняются |
| Notice → Notification, Appeal, Document | 1 : 0..N | Каскадное удаление вместе с оповещением |
| Notification → DeliveryAttempt | 1 : 0..N | Журнал попыток; итог — в `Notifications.Status` |
| Role → RolePermission | 1 : 1..N | Каскадное удаление; CHECK на допустимые коды прав |
| Role → AppUser | 1 : 0..N | `ON UPDATE CASCADE`, `ON DELETE RESTRICT` — роль с пользователями не удалить |
| Recipient → AppUser | 1 : 0..1 | Обязательна для ролей «только свои данные»; для `Observer` — CHECK `CK_AppUsers_Observer_Recipient` |

## Индексы и ограничения

* **Уникальные:** `Recipients.ExternalIdentifier`, `Offices.Name`, `Notices.Number`, `Addresses.RecipientId`,
  `AppUsers.Username` (PK), `Roles.Name` (PK), `Broadcasts.Code`.
* **CHECK:** `Notices.ProcessingDeadline >= CreatedAt`; непустая `Notices.Reason`; допустимые значения
  `Notices.CurrentStatus`, `Notifications.Channel`, `Notifications.Status`, `DeliveryAttempts.Status`,
  `RolePermissions.Permission`, `Broadcasts.Severity`, `Broadcasts.Status`; формат `Roles.Name`; непустой `Broadcasts.Title`.
* **Для запросов:** `Notices (RecipientId)`, `(OfficeId)`, `(RecipientId, CreatedAt)`, `(OfficeId, CurrentStatus)`,
  `(CurrentStatus, CreatedAt)`, `(BroadcastId, CurrentStatus)`; `Notifications (NoticeId)`, `(NoticeId, Status)`, `(CreatedAt)`;
  `DeliveryAttempts (NotificationId, AttemptedAt)`; `NoticeStatusHistories (NoticeId, ChangedAt)`;
  `AuditEvents (NoticeId, OccurredAt)`; `Appeals (NoticeId)`; `Documents (NoticeId)`; `Recipients (OfficeId)`;
  `Broadcasts (Status)`; `AppUsers (Role)`.
* **Параллельные изменения:** обновление `Notices` проверяет системный столбец PostgreSQL `xmin`; при расхождении — 409.

Типы: даты — `date` (`DateOnly`), моменты времени — `timestamptz` (`DateTimeOffset`), перечисления хранятся строками.
