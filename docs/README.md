# Документация АДИМАРИП

Состояние на 20.11.2026 (выпуск №70). Все документы раздела «Актуальные» соответствуют коду в репозитории.

## Актуальные

| Документ | О чём | Кому |
|---|---|---|
| [ТЗ.MD](ТЗ.MD) | Техническое задание (ГОСТ 34.602), редакция 2.0 | преподаватель, заказчик |
| [business_requirements.md](business_requirements.md) | Бизнес-требования: цели, роли, процессы, правила (ред. 2.0) | заказчик, аналитик |
| [system_requirements.md](system_requirements.md) | Системные требования с идентификаторами SR-* (v1.0) | разработчики, тестировщики |
| [UseCases_AND_Functions.md](UseCases_AND_Functions.md) | Варианты использования UC-00…UC-16 и функции | все |
| [API.md](API.md) | Контракт API: маршруты, права, тела запросов, коды ответов | frontend, тестировщики |
| [ARCHITECTURE.md](ARCHITECTURE.md) | Слои, паттерны проектирования, массовое оповещение, кастомные роли, инструменты качества | разработчики, защита |
| [erd.md](erd.md) | Схема БД: 15 таблиц, связи, ограничения, индексы | разработчики |
| [open_questions.md](open_questions.md) | Журнал решений команды и открытые вопросы | все |
| [ROADMAP_PRERELEASE.md](ROADMAP_PRERELEASE.md) | Дорожная карта M0–M12 и расхождения с ТЗ | команда |
| [RELEASE_70.md](RELEASE_70.md) | Что сделано в выпуске №70 и как это показать | преподаватель, команда |
| [ACCEPTANCE_MATRIX.md](ACCEPTANCE_MATRIX.md) | Контрольные проверки с ожидаемыми ответами | тестировщики |
| [QA_HANDOFF.md](QA_HANDOFF.md) | Команды проверки и чек-лист приёмки | тестировщики |
| [DATA_CONTRACT.md](DATA_CONTRACT.md) | Решения по хранению, статусам, уведомлениям, рассылкам | разработчики |
| [OBSERVER_ACCESS.md](OBSERVER_ACCESS.md) | Изоляция данных работника («только свои») | тестировщики, безопасность |
| [DEMO_CONFIGURATION.md](DEMO_CONFIGURATION.md) | Демо-данные, учётные записи, параметры имитации | все |
| [DATABASE_MIGRATIONS.md](DATABASE_MIGRATIONS.md) | Миграции и проверки целостности | разработчики, эксплуатация |
| [PRODUCTION_DEPLOYMENT.md](PRODUCTION_DEPLOYMENT.md) | HTTPS-профиль, секреты, обновление | эксплуатация |
| [CLOUD_DEPLOYMENT.md](CLOUD_DEPLOYMENT.md) | Размещение одним контейнером на netrun / Railway / Render | эксплуатация |

## Исторические (не обновляются)

| Документ | О чём |
|---|---|
| [seminar-2-status.md](seminar-2-status.md) | Состояние на чекпоинте 2 (13.10.2026) |
| [COMMIT_PLAN_25_PLUS.md](COMMIT_PLAN_25_PLUS.md) | План публикаций №25 и далее |
| [RELEASE_25-51.md](RELEASE_25-51.md), [RELEASE_55-69.md](RELEASE_55-69.md) | Заметки к прошлым выпускам |

## Как поддерживать актуальность

Изменение поведения API → обновить `API.md`, затронутые требования SR-* и строки `ACCEPTANCE_MATRIX.md`.
Новая миграция → `erd.md` и `DATABASE_MIGRATIONS.md`. Новое решение там, где требования молчат → `open_questions.md`.
Выпуск → новая заметка `RELEASE_<номер>.md` и отметка в `ROADMAP_PRERELEASE.md`.
