using static Adimarip.Domain.Access.Permission;

namespace Adimarip.Domain.Access;

/// <summary>
/// Четыре исходные роли проекта, выраженные через права.
/// Те же наборы записывает миграция 202611190006_RolesAndBroadcasts.
/// </summary>
public static class SystemRoles
{
    public const string Operator = "Operator";
    public const string Manager = "Manager";
    public const string Observer = "Observer";
    public const string AutomationEngineer = "AutomationEngineer";

    private static readonly Permission[] OperatorPermissions =
    [
        NoticesReadAll, NoticesCreate, NoticesChangeStatus, NotificationsSend, BroadcastsSend, BroadcastsView,
        AppealsCreate, DocumentsAttach, RecipientsRead, RecipientsArchive, HistoryView
    ];

    public static IReadOnlyList<Role> All { get; } =
    [
        Role.CreateSystem(Operator, "Секретарь", OperatorPermissions),
        Role.CreateSystem(Manager, "Управитель", [.. OperatorPermissions, AuditView]),
        Role.CreateSystem(Observer, "Служащий", [NoticesReadOwn, NoticesAcknowledgeOwn, DocumentsAttach]),
        Role.CreateSystem(AutomationEngineer, "Инженер автоматизации", [UsersManage, RolesManage, RecipientsCreate])
    ];

    public static Role Get(string name) => All.Single(r => r.Name == name);
}
