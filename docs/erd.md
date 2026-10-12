# ERD — реестр оповещений «АДИМАРИП»

**Статус:** проект модели данных для будущей системы. В исходном архиве уже есть `adimarip-efcore/Adimarip.Data/Entities/`, `AdimaripDbContext` и черновая миграция. **Таблицы не разворачиваются автоматически на семинаре 2.**

Диаграмма сопоставлена с текущими C#-сущностями: реальные ключи `int`/`long`, а идентификаторы пользователей пока строковые. `USER`/`ACCOUNT` и ролевое хранилище **ещё не реализованы**, связи с ними в БД отсутствуют. Это важное отличие от первоначального логического ERD с `uuid`.

```mermaid
erDiagram
    RECIPIENT {
        int Id PK
        varchar ExternalIdentifier UK
        varchar FullName
        date DateOfBirth
        varchar Email "nullable"
        varchar Phone "nullable"
    }
    ADDRESS {
        int Id PK
        int RecipientId FK,UK
        varchar PostalCode "nullable"
        varchar Region "nullable"
        varchar City "nullable"
        varchar Street "nullable"
        varchar House "nullable"
        varchar Apartment "nullable"
    }
    OFFICE {
        int Id PK
        varchar Name UK
    }
    NOTICE {
        int Id PK
        varchar Number UK
        int RecipientId FK
        int OfficeId FK
        varchar AuthorUserId "не FK до появления Account"
        date CreatedAt
        date ProcessingDeadline
        varchar Reason
        varchar Comment "nullable"
        varchar CurrentStatus "по умолчанию Created"
    }
    NOTICE_STATUS_HISTORY {
        bigint Id PK
        int NoticeId FK
        varchar Status
        varchar Comment "nullable"
        varchar ChangedByUserId "не FK"
        timestamptz ChangedAt
    }
    AUDIT_EVENT {
        bigint Id PK
        int NoticeId FK
        varchar Action
        varchar UserId "не FK"
        timestamptz OccurredAt
        varchar ObjectType "nullable"
        varchar ObjectId "nullable"
        varchar Details "nullable"
    }
    NOTIFICATION {
        int Id PK
        int NoticeId FK
        varchar Channel
        varchar RecipientAddress
        timestamptz CreatedAt
    }
    DELIVERY_ATTEMPT {
        bigint Id PK
        int NotificationId FK
        varchar Status "демо значение Delivered"
        timestamptz AttemptedAt
    }
    APPEAL {
        int Id PK
        int NoticeId FK
        varchar Type
        text Text
        varchar Status "демо значение Submitted"
        timestamptz SubmittedAt
    }
    DOCUMENT {
        int Id PK
        int NoticeId FK
        varchar Name
        varchar MimeType
        varchar StorageUri
        timestamptz AddedAt
    }
    RECIPIENT ||--o| ADDRESS : "адрес"
    RECIPIENT ||--o{ NOTICE : "получает"
    OFFICE ||--o{ NOTICE : "обрабатывает"
    NOTICE ||--o{ NOTICE_STATUS_HISTORY : "история"
    NOTICE ||--o{ AUDIT_EVENT : "аудит"
    NOTICE ||--o{ NOTIFICATION : "уведомления"
    NOTIFICATION ||--o{ DELIVERY_ATTEMPT : "попытки"
    NOTICE ||--o{ APPEAL : "обращения"
    NOTICE ||--o{ DOCUMENT : "метаданные"
```

## Кардинальности и ограничения

| Связь | Кратность существующей модели | Ограничения |
|---|---|---|
| Recipient → Address | 1 : 0..1 на уровне БД | `Address.RecipientId` уникален; бизнес-процесс создания адресата предполагает обязательный адрес в будущем API |
| Recipient → Notice | 1 : 0..N | Нельзя создать Notice без существующего Recipient |
| Office → Notice | 1 : 0..N | Нельзя создать Notice без существующего Office |
| Notice → StatusHistory / AuditEvent | 1 : 0..N | После внедрения CRUD история и аудит должны записываться атомарно с изменением статуса |
| Notice → Notification / Appeal / Document | 1 : 0..N | Содержимое документов не загружается |
| Notification → DeliveryAttempt | 1 : 0..N | Будущая отправка демонстрационная, без внешнего SMS/e-mail провайдера |

Уникальные индексы: `Recipient.ExternalIdentifier`, `Office.Name`, `Notice.Number`, `Address.RecipientId`. В `AdimaripDbContext` заданы ограничения `Notice.ProcessingDeadline >= Notice.CreatedAt` и непустая `Notice.Reason`. Даты хранятся как `DateOnly`, время событий — `DateTimeOffset`.


> Модель ролей и нерешённые вопросы будут дополнены в следующей итерации ERD.
