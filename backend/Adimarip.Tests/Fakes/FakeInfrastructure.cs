using Adimarip.Application.Abstractions;
using Adimarip.Application.Access;
using Adimarip.Application.Broadcasts;
using Adimarip.Application.Delivery;
using Adimarip.Application.Directory;
using Adimarip.Application.Notices;
using Adimarip.Domain.Access;
using Adimarip.Domain.Broadcasts;
using Adimarip.Domain.Common;
using Adimarip.Domain.Directory;
using Adimarip.Domain.Notices;

namespace Adimarip.Tests.Fakes;

internal sealed class FixedClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; set; } = now;
}

/// <summary>In-memory реализация репозиториев и единицы работы для тестов сервисов.</summary>
internal sealed class InMemoryStore : INoticeRepository, IRecipientRepository, IOfficeRepository, IBroadcastRepository,
    IRoleRepository, IUserRepository, IUnitOfWork
{
    private int _nextId = 1000;
    public List<Notice> Notices { get; } = new();
    public List<Recipient> Recipients { get; } = new();
    public List<Office> Offices { get; } = new() { new Office("Тестовый отдел").WithId(1) };
    public List<Broadcast> Broadcasts { get; } = new();
    public List<Role> Roles { get; } = SystemRoles.All.ToList();
    public List<AppUser> Users { get; } = new();
    public int SaveCount { get; private set; }
    public Func<int, bool>? FailSaveNumber { get; set; }

    // IUnitOfWork
    public Task SaveChangesAsync(CancellationToken ct)
    {
        SaveCount++;
        if (FailSaveNumber?.Invoke(SaveCount) == true) throw new PersistenceConflictException("conflict");
        foreach (var b in Broadcasts.Where(b => b.Id == 0)) b.WithId(_nextId++);
        foreach (var n in Notices.Where(n => n.Id == 0)) n.WithId(_nextId++);
        foreach (var r in Recipients.Where(r => r.Id == 0)) r.WithId(_nextId++);
        return Task.CompletedTask;
    }
    public void DiscardChanges() { }
    public async Task<TResult> InTransactionAsync<TResult>(Func<Task<TResult>> action, TransactionMode mode, CancellationToken ct)
        where TResult : Result => await action();

    // INoticeRepository
    public Task<PagedResult<NoticeListItem>> SearchAsync(NoticeSearch criteria, CancellationToken ct) => throw new NotSupportedException();
    public Task<Notice?> GetDetailsAsync(int id, int? owner, CancellationToken ct) => GetForUpdateAsync(id, owner, ct);
    public Task<Notice?> GetForUpdateAsync(int id, int? owner, CancellationToken ct) =>
        Task.FromResult(Notices.FirstOrDefault(n => n.Id == id && (owner is null || n.RecipientId == owner)));
    public Task<IReadOnlyList<Notice>> GetManyForUpdateAsync(IReadOnlyCollection<int> ids, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Notice>>(Notices.Where(n => ids.Contains(n.Id)).ToList());
    public Task<bool> NumberExistsAsync(string number, CancellationToken ct) => Task.FromResult(Notices.Any(n => n.Number == number));
    public Task<IReadOnlyList<Notice>> GetUndeliveredBroadcastBatchAsync(int broadcastId, int take, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Notice>>(Notices
            .Where(n => n.BroadcastId == broadcastId &&
                        (n.CurrentStatus == NoticeStatus.Created || n.Notifications.Any(x => x.IsPending)))
            .OrderBy(n => n.Id).Take(take).ToList());
    public Task<BroadcastProgress> GetBroadcastProgressAsync(int broadcastId, CancellationToken ct)
    {
        var all = Notices.Where(n => n.BroadcastId == broadcastId).ToList();
        var delivered = all.Count(n => n.Notifications.Any(x => x.Status == NotificationStatus.Delivered));
        var pending = all.Count(n =>
            n.Notifications.All(x => x.Status != NotificationStatus.Delivered) && n.Notifications.Any(x => x.IsPending));
        var acknowledged = all.Count(n => n.CurrentStatus is NoticeStatus.Read or NoticeStatus.Closed);
        return Task.FromResult(new BroadcastProgress(all.Count, delivered, all.Count - delivered - pending, pending, acknowledged, []));
    }
    public Task<IReadOnlyList<BroadcastFailure>> GetBroadcastFailuresAsync(int broadcastId, int take, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<BroadcastFailure>>([]);
    public void Add(Notice notice) => Notices.Add(notice);

    // IRecipientRepository
    public Task<IReadOnlyList<RecipientListItem>> SearchActiveAsync(string? term, int? only, int take, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<RecipientListItem>>(Recipients.Where(r => !r.IsArchived && (only is null || r.Id == only))
            .Select(r => new RecipientListItem(r.Id, r.ExternalIdentifier, r.FullName, r.DateOfBirth, r.Email, r.Phone, r.OfficeId, null))
            .ToList());
    public Task<Recipient?> GetForUpdateAsync(int id, CancellationToken ct) => Task.FromResult(Recipients.FirstOrDefault(r => r.Id == id));
    public Task<RecipientState> GetStateAsync(int id, CancellationToken ct)
    {
        var r = Recipients.FirstOrDefault(x => x.Id == id);
        return Task.FromResult(r is null ? RecipientState.Missing : r.IsArchived ? RecipientState.Archived : RecipientState.Active);
    }
    public Task<string?> GetFullNameAsync(int id,
            CancellationToken ct) => Task.FromResult(Recipients.FirstOrDefault(r => r.Id == id)?.FullName);
    public Task<bool> IdentifierExistsAsync(string identifier, CancellationToken ct) =>
        Task.FromResult(Recipients.Any(r => r.ExternalIdentifier == identifier));
    public Task<int> CountAudienceAsync(BroadcastAudience audience, CancellationToken ct) => Task.FromResult(Audience(audience).Count());
    public Task<IReadOnlyList<RecipientContacts>> GetAudienceAsync(BroadcastAudience audience, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<RecipientContacts>>(Audience(audience)
            .Select(r => new RecipientContacts(r.Id, r.FullName, r.Email, r.Phone)).ToList());
    public void Add(Recipient recipient) => Recipients.Add(recipient);
    private IEnumerable<Recipient> Audience(BroadcastAudience a) => Recipients.Where(r => !r.IsArchived).Where(r => a.Kind switch
    {
        AudienceKind.Offices => r.OfficeId is int o && a.OfficeIds.Contains(o),
        AudienceKind.Recipients => a.RecipientIds.Contains(r.Id),
        _ => true
    });

    // IOfficeRepository
    public Task<IReadOnlyList<OfficeItem>> ListAsync(int? only, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<OfficeItem>>(Offices.Select(o => new OfficeItem(o.Id, o.Name)).ToList());
    public Task<bool> ExistsAsync(int id, CancellationToken ct) => Task.FromResult(Offices.Any(o => o.Id == id));

    // IBroadcastRepository
    Task<Broadcast?> IBroadcastRepository.GetForUpdateAsync(int id,
            CancellationToken ct) => Task.FromResult(Broadcasts.FirstOrDefault(b => b.Id == id));
    public Task<IReadOnlyList<Broadcast>> ListRecentAsync(int take,
            CancellationToken ct) => Task.FromResult<IReadOnlyList<Broadcast>>(Broadcasts);
    public Task<Broadcast?> GetAsync(int id, CancellationToken ct) => Task.FromResult(Broadcasts.FirstOrDefault(b => b.Id == id));
    public Task<IReadOnlyList<(int Id, BroadcastSeverity Severity)>> GetUnfinishedAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<(int, BroadcastSeverity)>>(Broadcasts.Where(b => !b.IsFinished).Select(b => (b.Id,
                b.Severity)).ToList());
    public void Add(Broadcast broadcast) => Broadcasts.Add(broadcast);

    // IRoleRepository
    Task<IReadOnlyList<Role>> IRoleRepository.ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Role>>(Roles);
    Task<Role?> IRoleRepository.GetAsync(string name, CancellationToken ct) => Task.FromResult(Roles.FirstOrDefault(r => r.Name == name));
    Task<bool> IRoleRepository.ExistsAsync(string name, CancellationToken ct) =>
        Task.FromResult(Roles.Any(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase)));
    public void Add(Role role) => Roles.Add(role);
    public void Remove(Role role) => Roles.Remove(role);

    // IUserRepository
    public Task<AppUser?> FindAsync(string username,
            CancellationToken ct) => Task.FromResult(Users.FirstOrDefault(u => u.Username == username));
    public Task<AppUser?> FindForLoginAsync(string username, CancellationToken ct) =>
        Task.FromResult(Users.FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase)));
    Task<IReadOnlyList<UserListItem>> IUserRepository.ListAsync(CancellationToken ct) => throw new NotSupportedException();
    public Task<int> CountWithRoleAsync(string roleName, bool withoutRecipientOnly, CancellationToken ct) =>
        Task.FromResult(Users.Count(u => u.RoleName == roleName && (!withoutRecipientOnly || u.RecipientId is null)));
    public Task<IReadOnlyDictionary<string, int>> CountByRoleAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyDictionary<string, int>>(Users.GroupBy(u => u.RoleName).ToDictionary(g => g.Key, g => g.Count()));
    public void Add(AppUser user) => Users.Add(user);
}

internal sealed class NoCacheRoles(IRoleRepository roles) : IRoleSnapshotProvider
{
    public List<string> Invalidated { get; } = new();
    public async Task<RoleSnapshot?> GetAsync(string roleName, CancellationToken ct) =>
        await roles.GetAsync(roleName, ct) is { } role ? RoleSnapshot.From(role) : null;
    public void Invalidate(string roleName) => Invalidated.Add(roleName);
}

internal sealed class RecordingQueue : IBroadcastQueue
{
    public List<(int Id, BroadcastSeverity Severity)> Items { get; } = new();
    public void Enqueue(int broadcastId, BroadcastSeverity severity) => Items.Add((broadcastId, severity));
    public ValueTask<int> DequeueAsync(CancellationToken ct) => throw new NotSupportedException();
}

/// <summary>Канал, отвечающий по заранее заданному сценарию.</summary>
internal sealed class ScriptedSender(DeliveryChannel channel, params SendOutcome[] script) : IChannelSender
{
    private int _calls;
    public DeliveryChannel Channel { get; } = channel;
    public bool IsSimulated => true;
    public int Calls => _calls;
    public Func<OutgoingMessage, SendOutcome>? ByMessage { get; init; }

    public Task<SendOutcome> SendAsync(OutgoingMessage message, CancellationToken ct)
    {
        var call = Interlocked.Increment(ref _calls) - 1;
        if (ByMessage is not null) return Task.FromResult(ByMessage(message));
        return Task.FromResult(script.Length == 0 ? SendOutcome.Success : script[Math.Min(call, script.Length - 1)]);
    }
}

internal sealed class PlainHasher : IPasswordHasher
{
    public string Hash(string password) => "hash:" + password;
    public bool Verify(string hash, string password) => hash == "hash:" + password;
}
