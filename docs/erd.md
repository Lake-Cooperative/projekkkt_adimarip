Диаграмма построена по разделу 6 [`docs/system-requirements.md`](system-requirements.md).

> **Что подтверждено, а что нет.** Названия сущностей, их связи и перечисленные атрибуты следуют из бизнес-требований и use cases. Суррогатные ключи `id`, внешние ключи, типы данных и `NOT NULL` не заданы в источниках — это **предложение** для проектирования схемы PostgreSQL. Статусы и их перечень пока не определены, поэтому они показаны как строки.

```mermaid
erDiagram
    USER {
        uuid id PK
        varchar login UK "уникальный логин"
        varchar password_hash
        varchar display_name
        varchar role "Operator | Manager | Observer | AutomationEngineer"
    }

    RECIPIENT {
        uuid id PK
        varchar external_id UK "уникальный идентификатор адресата"
        varchar full_name "ФИО"
        date birth_date
        varchar email
        varchar phone
    }

    ADDRESS {
        uuid id PK
        uuid recipient_id FK,UK "1:1 с адресатом"
        varchar postal_code "индекс"
        varchar region
        varchar city
        varchar street
        varchar house
        varchar apartment "необязательно"
    }

    OFFICE {
        uuid id PK
        varchar name "подразделение"
    }

    NOTICE {
        uuid id PK
        varchar number UK "уникальный номер оповещения"
        uuid recipient_id FK
        uuid office_id FK
        uuid author_id FK "пользователь-автор"
        date created_on "дата создания"
        date due_on "срок обработки, не раньше created_on"
        varchar reason "причина, не пустая"
        text comment "необязательно"
        varchar status "текущий статус"
    }

    NOTICE_STATUS_HISTORY {
        uuid id PK
        uuid notice_id FK
        varchar status
        text comment
        uuid changed_by FK
        timestamptz changed_at
    }

    AUDIT_EVENT {
        uuid id PK
        uuid notice_id FK "может быть пустым для событий вне оповещения"
        uuid user_id FK
        varchar action "например, NoticeCreated, StatusChanged"
        timestamptz occurred_at
    }

    NOTIFICATION {
        uuid id PK
        uuid notice_id FK
        varchar channel "канал, из допустимого перечня"
        varchar target "адрес получателя"
        timestamptz created_at
    }

    DELIVERY_ATTEMPT {
        uuid id PK
        uuid notification_id FK
        varchar status "в демо всегда Delivered"
        timestamptz attempted_at
    }

    APPEAL {
        uuid id PK
        uuid notice_id FK
        varchar type "тип обращения, например Clarification"
        text body "текст, не пустой"
        varchar status "при создании Submitted"
        timestamptz created_at
    }

    DOCUMENT {
        uuid id PK
        uuid notice_id FK
        varchar name
        varchar mime_type
        varchar storage_uri "URI или идентификатор хранилища"
    }

    RECIPIENT ||--|| ADDRESS : "имеет адрес"
    RECIPIENT ||--o{ NOTICE : "получает"
    OFFICE ||--o{ NOTICE : "обрабатывает"
    USER ||--o{ NOTICE : "создаёт"
    NOTICE ||--|{ NOTICE_STATUS_HISTORY : "история статусов"
    USER ||--o{ NOTICE_STATUS_HISTORY : "меняет статус"
    NOTICE ||--o{ AUDIT_EVENT : "аудит"
    USER ||--o{ AUDIT_EVENT : "действие пользователя"
    NOTICE ||--o{ NOTIFICATION : "уведомления"
    NOTIFICATION ||--|{ DELIVERY_ATTEMPT : "попытки доставки"
    NOTICE ||--o{ APPEAL : "обращения"
    NOTICE ||--o{ DOCUMENT : "документы"
```

## Пояснения к связям

| Связь | Кратность | Обоснование |
|---|---|---|
| Recipient — Address | 1 : 1 | UC-01: создаются записи `Recipient` и `Address`, адрес вложен в адресата |
| Recipient — Notice | 1 : 0..N | Адресат может получить несколько оповещений; оповещение всегда имеет адресата (БП-5) |
| Office — Notice | 1 : 0..N | При создании оповещения подразделение должно существовать (БП-5) |
| Notice — NoticeStatusHistory | 1 : 1..N | При создании оповещения сразу пишется начальная запись истории (UC-02) |
| Notice — AuditEvent | 1 : 0..N | Аудит пишется при создании и смене статуса (UC-02, UC-05) |
| Notification — DeliveryAttempt | 1 : 1..N | Каждое уведомление сразу получает попытку доставки со статусом `Delivered` (UC-06) |
| Notice — Notification / Appeal / Document | 1 : 0..N | Привязываются к оповещению (UC-06, UC-07, UC-08) |
| User — Notice / StatusHistory / AuditEvent | 1 : 0..N | Автор оповещения, автор смены статуса, субъект аудита; в UC это «автор» и «аудит действий пользователей» |

## Ограничения, которые не видны на диаграмме

- `RECIPIENT.external_id` и `NOTICE.number` уникальны (БП-1, БП-2).
- `NOTICE.due_on >= NOTICE.created_on` (БП-3), `NOTICE.reason <> ''` (БП-4), `APPEAL.body <> ''` (БП-8) — проверки на уровне CHECK или приложения.
- Новый статус оповещения не равен текущему (БП-6) — проверяется в приложении.
- `NOTICE_STATUS_HISTORY` и `AUDIT_EVENT` только добавляются, без изменения и удаления (SR-STS-05).
- Допустимые значения `NOTIFICATION.channel` и `APPEAL.type` не определены (БТ §7).

## Что стоит подтвердить

1. **`USER` и связь с оповещением.** UC говорят, что автор передаётся во входных данных оповещения. В системных требованиях предложено брать его из JWT (SR-NTC-09). На диаграмме автор — внешний ключ на `USER`.
2. **`AUDIT_EVENT.notice_id`.** В UC аудит описан только для действий над оповещениями. Поле допускает пустое значение на случай событий вне оповещений (например, создание учётной записи). Если такие события не нужны, поле можно сделать обязательным.
3. **Статусы** — отдельный справочник (`STATUS`) или строковое поле. Пока перечень не согласован, выбрана строка; если статусов станет много или им понадобятся свойства, лучше завести справочник.
4. **Один адрес на адресата.** Источники описывают «вложенный адрес», поэтому выбрана связь 1:1. Если адресату нужно несколько адресов, связь станет 1:N.
