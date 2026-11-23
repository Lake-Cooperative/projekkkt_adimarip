# API АДИМАРИП (актуально на 20.11.2026)

Базовый адрес: `http://localhost:8081` (локально) или тот же домен, что и интерфейс (prod-профиль, путь `/api`).
Все маршруты, кроме входа и проверок готовности, требуют заголовок `Authorization: Bearer <JWT>`.
Формат — JSON, имена полей в `camelCase`, даты — `YYYY-MM-DD`, время — ISO 8601 с часовым поясом.
Интерактивное описание — Swagger UI по пути `/swagger` (только `ASPNETCORE_ENVIRONMENT=Development`).

## Общие правила

**Ошибки.** Тело ошибки всегда одинаковое: `{ "error": "Текст для пользователя" }`.

| Код | Когда |
|---|---|
| 400 | Некорректные данные (формат, длина, даты, неизвестный статус/канал/право, пустая аудитория) |
| 401 | Нет токена, токен истёк, неверный логин или пароль |
| 403 | У роли нет нужного права; роль «только свои данные» без привязки к адресату |
| 404 | Запись не найдена **или не видна** пользователю (чужое оповещение работника — тоже 404) |
| 409 | Конфликт бизнес-правил: дубликат, недопустимый переход статуса, параллельное изменение, роль используется |
| 429 | Превышен лимит попыток входа (10 в минуту с одного IP) |
| 501 | Отправка недоступна: провайдер доставки не настроен (`DEMO_ENABLED=false`) |

**Права.** Доступ к маршруту выдаётся, если у роли пользователя есть **хотя бы одно** из перечисленных прав
(список прав — `GET /api/permissions`, описание модели — [`ARCHITECTURE.md`](ARCHITECTURE.md#4-кастомные-роли)).
Права читаются из БД на каждый запрос, поэтому изменение роли действует без повторного входа.

**Область данных.** Право `NoticesReadAll` — весь реестр; `NoticesReadOwn` без `NoticesReadAll` — только записи
адресата, к которому привязана учётная запись. Ограничение применяется в SQL, а не в интерфейсе.

## Служебные

| Метод и путь | Доступ | Ответ |
|---|---|---|
| `GET /api/health` | без входа | `200 { status: "ok", service: "adimarip-api" }` |
| `GET /api/ready` | без входа | `200 { status: "ready", database: "connected" }` или `503` |

## Вход

| Метод и путь | Доступ | Тело запроса | Ответ |
|---|---|---|---|
| `POST /api/auth/login` | без входа | `{ username, password }` | `200` сессия; `400` пустые поля; `401` неверные данные; `429` |
| `GET /api/auth/me` | любой вошедший | — | `200` сессия без `token` и `expiresAt` |

Сессия: `{ token, expiresAt, username, displayName, role, roleDisplayName, recipientId, surname, permissions[] }`.
Логин не чувствителен к регистру; срок жизни токена — 2 часа.

## Оповещения

| Метод и путь | Права | Тело / параметры | Ответ |
|---|---|---|---|
| `GET /api/notices` | `NoticesReadAll`, `NoticesReadOwn` | `?status=&officeId=&search=&page=1&pageSize=15` | `200 { items[], total, page, pageSize, totalPages }`; `400` при `pageSize` вне 1–100 |
| `GET /api/notices/{id}` | `NoticesReadAll`, `NoticesReadOwn` | — | `200` карточка; `404` |
| `POST /api/notices` | `NoticesCreate` | `{ number, recipientId, officeId, createdAt?, processingDeadline, reason, comment? }` | `201 { id, number, currentStatus }`; `400`; `404`; `409` |
| `PATCH /api/notices/{id}/status` | `NoticesChangeStatus` | `{ status, comment? }` | `200 { id, currentStatus }`; `400` неизвестный статус; `404`; `409` переход запрещён или запись изменена параллельно |
| `POST /api/notices/{id}/acknowledge` | `NoticesAcknowledgeOwn` | — | `200 { id, currentStatus: "Read" }`; `403` нет привязки; `404` чужое; `409` уже подтверждено |
| `POST /api/notices/{id}/notifications` | `NotificationsSend` | `{ channel, recipientAddress }` | `201 { id, channel, simulated, status }`; `400`; `404`; `501` |
| `POST /api/notices/notifications/bulk` | `NotificationsSend` | `{ noticeIds[1..50], channel, recipientAddress }` | `200 { simulated, count, delivered, failed }`; `400`; `404`; `501` |
| `POST /api/notices/{id}/appeals` | `AppealsCreate` | `{ type, text }` | `201 { id, status: "Submitted" }`; `400`; `404` |
| `POST /api/notices/{id}/documents` | `DocumentsAttach` | `{ name, mimeType, storageUri }` (только `https://`) | `201 { id, name }`; `400`; `404` (у работника — и для чужого оповещения) |

Элемент списка: `{ id, number, recipientId, recipientName, officeId, officeName, createdAt, processingDeadline, reason,
comment, currentStatus, broadcastId, severity }` (`broadcastId`/`severity` заполнены у оповещений массовой рассылки).

Карточка — элемент списка плюс:

| Поле | Содержимое |
|---|---|
| `allowedTransitions[]` | Разрешённые следующие статусы (пусто без `NoticesChangeStatus`) |
| `canAcknowledge` | Можно ли нажать «Ознакомлен(а)» |
| `history[]` | `{ id, status, comment, changedAt, changedByUserId }` — только при `HistoryView`, иначе `[]` |
| `audit[]` | `{ id, action, userId, occurredAt, details }` — только при `AuditView`, иначе `null` |
| `notifications[]` | `{ id, channel, recipientAddress, createdAt, status, lastError, attempts[{ status, attemptedAt }] }` |
| `appeals[]`, `documents[]` | Обращения и метаданные документов |

**Правила создания.** Номер 3–100 символов и уникален; причина 1–1000; комментарий до 4000; `createdAt` не раньше
трёх дней назад (по умолчанию — сегодня); срок не раньше даты создания; адресат не списан.

**Статусы и переходы** (`Domain/Notices/NoticeLifecycle.cs`):

```
Created → Sent | Cancelled          Sent → Delivered | Failed | Cancelled
Failed  → Sent | Cancelled          Delivered → Read | Closed
Read    → Closed                    Closed, Cancelled — конечные
```

Значения статусов, каналов (`Email`, `Sms`, `Portal`, `Max`), важности и прав принимаются **только точным именем**
(регистр важен, числа не принимаются).

## Массовое оповещение

| Метод и путь | Права | Тело | Ответ |
|---|---|---|---|
| `POST /api/broadcasts/preview` | `BroadcastsSend` | `{ kind, officeIds?, recipientIds? }` | `200 { recipients, maxAllowed }`; `400` |
| `POST /api/broadcasts` | `BroadcastsSend` | см. ниже | `202 { id, code, status, targetCount, plannedNotifications }`, заголовок `Location`; `400`; `404`; `409`; `501` |
| `GET /api/broadcasts` | `BroadcastsView` | — | `200` последние 50 рассылок |
| `GET /api/broadcasts/{id}` | `BroadcastsView` | — | `200 { broadcast, progress, failures[] }`; `404` |

Тело запуска:

```json
{
  "title": "Учебная эвакуация корпуса Б",
  "message": "Покиньте здание по ближайшей лестнице",
  "severity": "Critical",
  "channels": ["Email", "Sms"],
  "officeId": 1,
  "audience": { "kind": "Offices", "officeIds": [1, 2], "recipientIds": [] }
}
```

* `severity`: `Info`, `Warning`, `Critical`. `Critical` — вне очереди и по всем каналам, где у работника есть контакт.
* `audience.kind`: `All` (все действующие адресаты), `Offices` (по `Recipients.OfficeId`), `Recipients` (список ID).
  Списанные адресаты не оповещаются. Не более 2000 человек за рассылку.
* Ответ `202` означает, что рассылка и оповещения каждому работнику сохранены, а доставка идёт в фоне.
* `progress`: `{ total, delivered, failed, pending, acknowledged, channels[{ channel, delivered, failed, pending }] }`.
* `failures[]` (до 100): `{ noticeId, recipientName, channel, address, error }`.
* Статусы рассылки: `Queued` → `Dispatching` → `Completed` | `CompletedWithErrors`.

## Справочники

| Метод и путь | Права | Тело / параметры | Ответ |
|---|---|---|---|
| `GET /api/offices` | `NoticesReadAll`, `NoticesReadOwn`, `BroadcastsSend`, `UsersManage` | — | `200 [{ id, name }]` (работнику — только подразделения его оповещений) |
| `GET /api/recipients` | `RecipientsRead`, `NoticesReadOwn` | `?search=` | `200` до 100 действующих адресатов (работнику — только своя карточка) |
| `POST /api/recipients` | `RecipientsCreate` | `{ externalIdentifier, fullName, dateOfBirth, email?, phone?, officeId?, address? }` | `201 { id, fullName, externalIdentifier }`; `400`; `404` подразделение; `409` дубликат |
| `POST /api/recipients/{id}/archive` | `RecipientsArchive` | `{ reason }` (3–1000) | `200 { recipientId, archivedAt, reason }`; `400`; `404`; `409` уже списан |

## Учётные записи и роли

| Метод и путь | Права | Тело | Ответ |
|---|---|---|---|
| `GET /api/admin/users` | `UsersManage` | — | `200 [{ username, displayName, role, recipientId, createdAt }]` |
| `POST /api/admin/users` | `UsersManage` | `{ username, displayName, role, password, dateOfBirth?, email?, phone?, city?, externalIdentifier?, officeId? }` | `201 { username, role, recipientId }`; `400`; `404`; `409` |
| `PUT /api/admin/users/{username}/role` | `UsersManage` | `{ role }` | `200` учётная запись; `400`; `404`; `409` (самоблокировка, нет привязки для роли «только свои») |
| `GET /api/permissions` | `RolesManage`, `UsersManage` | — | `200 [{ code, group, description }]` |
| `GET /api/roles` | `RolesManage`, `UsersManage` | — | `200 [{ name, displayName, isSystem, scope, requiresRecipientBinding, permissions[], usersCount }]` |
| `POST /api/roles` | `RolesManage` | `{ name, displayName, permissions[] }` | `201`; `400`; `409` код занят |
| `PUT /api/roles/{name}` | `RolesManage` | `{ displayName, permissions[] }` | `200`; `400`; `404`; `409` (системная роль, самоблокировка, пользователи без привязки) |
| `DELETE /api/roles/{name}` | `RolesManage` | — | `204`; `404`; `409` (системная или назначена пользователям) |

* Логин — 3–40 символов `[a-zA-Z0-9_.-]`, уникален без учёта регистра; пароль — не короче 10 символов.
* Если роль видит только свои данные (`requiresRecipientBinding = true`), обязательна `dateOfBirth`: вместе с
  учётной записью в одной транзакции создаётся адресат (идентификатор по умолчанию `EMP-<ЛОГИН>`), его ID возвращается.
* Код роли — `^[A-Za-z][A-Za-z0-9_]{2,39}$`; `NoticesReadAll` поглощает `NoticesReadOwn`.

## Системные роли

| Роль | Название | Права |
|---|---|---|
| `Operator` | Секретарь | `NoticesReadAll`, `NoticesCreate`, `NoticesChangeStatus`, `NotificationsSend`, `BroadcastsSend`, `BroadcastsView`, `AppealsCreate`, `DocumentsAttach`, `RecipientsRead`, `RecipientsArchive`, `HistoryView` |
| `Manager` | Управитель | все права секретаря + `AuditView` |
| `Observer` | Служащий | `NoticesReadOwn`, `NoticesAcknowledgeOwn`, `DocumentsAttach` |
| `AutomationEngineer` | Инженер автоматизации | `UsersManage`, `RolesManage`, `RecipientsCreate` |

Демо-данные дополнительно создают кастомную роль `DutyOfficer` («Дежурный по безопасности»):
`NoticesReadAll`, `RecipientsRead`, `HistoryView`, `BroadcastsSend`, `BroadcastsView`.
