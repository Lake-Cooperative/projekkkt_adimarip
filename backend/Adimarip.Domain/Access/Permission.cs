namespace Adimarip.Domain.Access;

/// <summary>
/// Атомарные права. Роль — это просто именованный набор прав, поэтому
/// инженер может собрать любую кастомную роль, не меняя код.
/// Имена хранятся в БД (таблица RolePermissions) — не переименовывать.
/// </summary>
public enum Permission
{
    NoticesReadAll,
    NoticesReadOwn,
    NoticesCreate,
    NoticesChangeStatus,
    NoticesAcknowledgeOwn,
    NotificationsSend,
    BroadcastsSend,
    BroadcastsView,
    AppealsCreate,
    DocumentsAttach,
    RecipientsRead,
    RecipientsCreate,
    RecipientsArchive,
    HistoryView,
    AuditView,
    UsersManage,
    RolesManage
}

public sealed record PermissionInfo(Permission Permission, string Group, string Description);

/// <summary>Справочник прав с описаниями для экрана редактирования ролей.</summary>
public static class PermissionCatalog
{
    public static IReadOnlyList<PermissionInfo> All { get; } =
    [
        new(Permission.NoticesReadAll, "Реестр", "Просмотр всех оповещений и справочника подразделений"),
        new(Permission.NoticesReadOwn, "Реестр", "Просмотр только своих оповещений (нужна привязка к адресату)"),
        new(Permission.NoticesCreate, "Реестр", "Создание оповещений"),
        new(Permission.NoticesChangeStatus, "Реестр", "Смена статуса оповещения"),
        new(Permission.NoticesAcknowledgeOwn, "Реестр", "Подтверждение получения своего оповещения"),
        new(Permission.HistoryView, "Реестр", "Просмотр истории статусов"),
        new(Permission.AuditView, "Реестр", "Просмотр журнала аудита"),
        new(Permission.NotificationsSend, "Уведомления", "Отправка уведомлений по отдельным оповещениям"),
        new(Permission.BroadcastsSend, "Уведомления", "Запуск массового оповещения работников"),
        new(Permission.BroadcastsView, "Уведомления", "Просмотр хода и отчётов массовых оповещений"),
        new(Permission.AppealsCreate, "Обращения и документы", "Регистрация обращений"),
        new(Permission.DocumentsAttach, "Обращения и документы", "Добавление ссылок на документы"),
        new(Permission.RecipientsRead, "Адресаты", "Просмотр справочника адресатов"),
        new(Permission.RecipientsCreate, "Адресаты", "Создание адресатов"),
        new(Permission.RecipientsArchive, "Адресаты", "Списание адресатов"),
        new(Permission.UsersManage, "Администрирование", "Создание учётных записей и назначение ролей"),
        new(Permission.RolesManage, "Администрирование", "Создание и изменение кастомных ролей")
    ];

}
