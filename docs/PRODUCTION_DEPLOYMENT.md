# Эксплуатационное развёртывание демонстрационного реестра

**Не обрабатывайте реальные оповещения и персональные данные.** Это только
инфраструктурный профиль с HTTPS; он **не** реализует настоящие каналы доставки,
государственную идентификацию и юридически значимый документооборот.

## Перед запуском

1. Требуются Linux-сервер с Docker Compose, доступные порты 80/443, DNS A/AAAA,
   указывающие на сервер, и возможность Caddy получить ACME-сертификат.
2. Создайте собственный `.env` (не коммитьте его в Git):

```env
APP_DOMAIN=registry.example.org
POSTGRES_DB=adimarip
POSTGRES_USER=adimarip
POSTGRES_PASSWORD=REPLACE_WITH_STRONG_RANDOM_DB_PASSWORD
JWT_SECRET=REPLACE_WITH_DISTINCT_RANDOM_SECRET_MIN_32_BYTES
BOOTSTRAP_ADMIN_USERNAME=initial-engineer
BOOTSTRAP_ADMIN_PASSWORD=REPLACE_WITH_DISTINCT_RANDOM_ADMIN_PASSWORD_MIN_16
```

3. Поменяйте **все** заглушки на разные случайные пароли. Подключите регулярные
   резервные копии PostgreSQL с проверкой восстановления, ограничьте вход по SSH,
   настройте firewall, ограничьте доступ к серверу и мониторинг.
4. Запуск: `docker compose -f docker_compose.prod.yml up -d --build`.
5. После первого входа инженера создайте нужные роли (системные уже есть, кастомные — в разделе «Роли и права»)
   и отдельные учётные записи, удалите
   `BOOTSTRAP_ADMIN_PASSWORD` / `BOOTSTRAP_ADMIN_USERNAME` из `.env` и перезапустите
   API. Настройте ротацию и хранение секретов вне репозитория.

## Проверка

```bash
docker compose -f docker_compose.prod.yml ps
curl -f https://registry.example.org/api/health
curl -f https://registry.example.org/api/ready
```

При `Demo__Enabled=false` демонстрационная доставка и массовое оповещение возвращают 501, а фоновый обработчик
рассылок не запускается. Не заменяйте этот ответ статусом «доставлено» без реальной интеграции: новый провайдер
подключается реализацией `IChannelSender` (см. [`ARCHITECTURE.md`](ARCHITECTURE.md)).
База и API во внешней сети портов не публикуют; запросы идут
`Caddy HTTPS -> Nginx frontend -> ASP.NET /api -> PostgreSQL`.

## Обновление существующей установки

Выпуск №70 добавляет миграцию `202611190006_RolesAndBroadcasts` (роли, рассылки, новые столбцы). Перед обновлением:
`scripts/backup-db.sh`, затем `docker compose -f docker_compose.prod.yml up -d --build`; миграция применится при старте API.
Очередь рассылок хранится в памяти процесса — запускайте **один** экземпляр API.

Канонический EF snapshot пока не восстановлен: перед следующими миграциями
читайте `docs/DATABASE_MIGRATIONS.md`. Автоматическое применение миграций при
старте API удобно для учебного стенда, но должно быть вынесено из web-процесса
перед серьёзной эксплуатацией.
