using Adimarip.Application.Access;
using Adimarip.Application.Broadcasts;
using Adimarip.Application.Directory;
using Adimarip.Application.Notices;
using Adimarip.Domain.Access;
using Adimarip.Domain.Broadcasts;
using Adimarip.Domain.Directory;
using Adimarip.Domain.Notices;

namespace Adimarip.Application.Abstractions;

// Репозитории (паттерн Repository): сервисы работают с агрегатами и готовыми
// read-моделями, не зная, как устроены запросы к PostgreSQL.

public interface INoticeRepository
{
    Task<PagedResult<NoticeListItem>> SearchAsync(NoticeSearch criteria, CancellationToken ct);
    /// <summary>Карточка целиком, только для чтения. ownerRecipientId ограничивает выборку в SQL.</summary>
    Task<Notice?> GetDetailsAsync(int id, int? ownerRecipientId, CancellationToken ct);
    /// <summary>Отслеживаемый агрегат без дочерних коллекций — для изменений.</summary>
    Task<Notice?> GetForUpdateAsync(int id, int? ownerRecipientId, CancellationToken ct);
    Task<IReadOnlyList<Notice>> GetManyForUpdateAsync(IReadOnlyCollection<int> ids, CancellationToken ct);
    Task<bool> NumberExistsAsync(string number, CancellationToken ct);
    /// <summary>Очередная порция оповещений рассылки, ещё не прошедших доставку (с уведомлениями).</summary>
    Task<IReadOnlyList<Notice>> GetUndeliveredBroadcastBatchAsync(int broadcastId, int take, CancellationToken ct);
    Task<BroadcastProgress> GetBroadcastProgressAsync(int broadcastId, CancellationToken ct);
    Task<IReadOnlyList<BroadcastFailure>> GetBroadcastFailuresAsync(int broadcastId, int take, CancellationToken ct);
    void Add(Notice notice);
}

public enum RecipientState
{
    Missing,
    Active,
    Archived
}

public interface IRecipientRepository
{
    Task<IReadOnlyList<RecipientListItem>> SearchActiveAsync(string? term, int? onlyRecipientId, int take, CancellationToken ct);
    Task<Recipient?> GetForUpdateAsync(int id, CancellationToken ct);
    Task<RecipientState> GetStateAsync(int id, CancellationToken ct);
    Task<string?> GetFullNameAsync(int id, CancellationToken ct);
    Task<bool> IdentifierExistsAsync(string identifier, CancellationToken ct);
    Task<int> CountAudienceAsync(BroadcastAudience audience, CancellationToken ct);
    Task<IReadOnlyList<RecipientContacts>> GetAudienceAsync(BroadcastAudience audience, CancellationToken ct);
    void Add(Recipient recipient);
}

public interface IOfficeRepository
{
    Task<IReadOnlyList<OfficeItem>> ListAsync(int? onlyWithNoticesOfRecipient, CancellationToken ct);
    Task<bool> ExistsAsync(int id, CancellationToken ct);
}

public interface IUserRepository
{
    Task<AppUser?> FindAsync(string username, CancellationToken ct);
    /// <summary>Поиск без учёта регистра; точное совпадение имеет приоритет.</summary>
    Task<AppUser?> FindForLoginAsync(string username, CancellationToken ct);
    Task<IReadOnlyList<UserListItem>> ListAsync(CancellationToken ct);
    Task<int> CountWithRoleAsync(string roleName, bool withoutRecipientOnly, CancellationToken ct);
    /// <summary>Количество учётных записей по каждой роли — одним запросом.</summary>
    Task<IReadOnlyDictionary<string, int>> CountByRoleAsync(CancellationToken ct);
    void Add(AppUser user);
}

public interface IRoleRepository
{
    Task<IReadOnlyList<Role>> ListAsync(CancellationToken ct);
    Task<Role?> GetAsync(string name, CancellationToken ct);
    Task<bool> ExistsAsync(string name, CancellationToken ct);
    void Add(Role role);
    void Remove(Role role);
}

public interface IBroadcastRepository
{
    Task<Broadcast?> GetForUpdateAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<Broadcast>> ListRecentAsync(int take, CancellationToken ct);
    Task<Broadcast?> GetAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<(int Id, BroadcastSeverity Severity)>> GetUnfinishedAsync(CancellationToken ct);
    void Add(Broadcast broadcast);
}

/// <summary>Проверка готовности хранилища (для /api/ready).</summary>
public interface IHealthProbe
{
    Task<bool> IsDatabaseAvailableAsync(CancellationToken ct);
}
