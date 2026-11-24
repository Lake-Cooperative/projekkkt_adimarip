using Adimarip.Application.Abstractions;
using Adimarip.Application.Broadcasts;
using Adimarip.Application.Notices;
using Adimarip.Domain.Broadcasts;
using Adimarip.Domain.Notices;
using Microsoft.EntityFrameworkCore;

namespace Adimarip.Infrastructure.Persistence.Repositories;

internal sealed class NoticeRepository(AdimaripDbContext db) : INoticeRepository
{
    public async Task<PagedResult<NoticeListItem>> SearchAsync(NoticeSearch criteria, CancellationToken ct)
    {
        var query = Visible(db.Notices.AsNoTracking(), criteria.OwnerRecipientId);
        if (criteria.Status is { } status) query = query.Where(n => n.CurrentStatus == status);
        if (criteria.OfficeId is { } officeId) query = query.Where(n => n.OfficeId == officeId);
        if (criteria.Text is { } text)
        {
            var pattern = LikePattern.Contains(text);
            query = query.Where(n => EF.Functions.ILike(n.Number, pattern) ||
                                     EF.Functions.ILike(n.Reason, pattern) ||
                                     EF.Functions.ILike(n.Recipient.FullName, pattern));
        }

        var total = await query.CountAsync(ct);
        var rows = await query
            .OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id)
            .Skip(criteria.Page.Skip).Take(criteria.Page.PageSize)
            .Select(n => new
            {
                n.Id,
                n.Number,
                n.RecipientId,
                RecipientName = n.Recipient.FullName,
                n.OfficeId,
                OfficeName = n.Office.Name,
                n.CreatedAt,
                n.ProcessingDeadline,
                n.Reason,
                n.Comment,
                n.CurrentStatus,
                n.BroadcastId,
                Severity = n.Broadcast != null ? n.Broadcast.Severity : (BroadcastSeverity?)null
            })
            .ToListAsync(ct);

        var items = rows.Select(r => new NoticeListItem(r.Id, r.Number, r.RecipientId, r.RecipientName, r.OfficeId,
            r.OfficeName, r.CreatedAt, r.ProcessingDeadline, r.Reason, r.Comment, r.CurrentStatus.ToString(),
            r.BroadcastId, r.Severity?.ToString())).ToArray();
        return new PagedResult<NoticeListItem>(items, total, criteria.Page.Page, criteria.Page.PageSize);
    }

    public Task<Notice?> GetDetailsAsync(int id, int? ownerRecipientId, CancellationToken ct) =>
        Visible(db.Notices.AsNoTracking(), ownerRecipientId)
            .AsSplitQuery()
            .Include(n => n.Recipient)
            .Include(n => n.Office)
            .Include(n => n.Broadcast)
            .Include(n => n.StatusHistory)
            .Include(n => n.AuditEvents)
            .Include(n => n.Notifications).ThenInclude(x => x.DeliveryAttempts)
            .Include(n => n.Appeals)
            .Include(n => n.Documents)
            .FirstOrDefaultAsync(n => n.Id == id, ct);

    public Task<Notice?> GetForUpdateAsync(int id, int? ownerRecipientId, CancellationToken ct) =>
        Visible(db.Notices, ownerRecipientId).FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task<IReadOnlyList<Notice>> GetManyForUpdateAsync(IReadOnlyCollection<int> ids, CancellationToken ct) =>
        await db.Notices.Where(n => ids.Contains(n.Id)).OrderBy(n => n.Id).ToListAsync(ct);

    public Task<bool> NumberExistsAsync(string number, CancellationToken ct) =>
        db.Notices.AnyAsync(n => n.Number == number, ct);

    public async Task<IReadOnlyList<Notice>> GetUndeliveredBroadcastBatchAsync(int broadcastId, int take, CancellationToken ct) =>
        await db.Notices
            .Where(n => n.BroadcastId == broadcastId &&
                        (n.CurrentStatus == NoticeStatus.Created ||
                         n.Notifications.Any(x => x.Status == NotificationStatus.Pending)))
            .OrderBy(n => n.Id)
            .Take(take)
            .Include(n => n.Notifications)
            .AsSplitQuery()
            .ToListAsync(ct);

    public async Task<BroadcastProgress> GetBroadcastProgressAsync(int broadcastId, CancellationToken ct)
    {
        var ofBroadcast = db.Notices.AsNoTracking().Where(n => n.BroadcastId == broadcastId);
        var total = await ofBroadcast.CountAsync(ct);
        var delivered = await ofBroadcast.CountAsync(n => n.Notifications.Any(x => x.Status == NotificationStatus.Delivered), ct);
        var pending = await ofBroadcast.CountAsync(n =>
            !n.Notifications.Any(x => x.Status == NotificationStatus.Delivered) &&
            n.Notifications.Any(x => x.Status == NotificationStatus.Pending), ct);
        var acknowledged = await ofBroadcast.CountAsync(n =>
            n.CurrentStatus == NoticeStatus.Read || n.CurrentStatus == NoticeStatus.Closed, ct);

        var byChannel = await db.Notifications.AsNoTracking()
            .Where(x => ofBroadcast.Any(n => n.Id == x.NoticeId))
            .GroupBy(x => new { x.Channel, x.Status })
            .Select(g => new { g.Key.Channel, g.Key.Status, Count = g.Count() })
            .ToListAsync(ct);

        var channels = byChannel
            .GroupBy(x => x.Channel)
            .OrderBy(g => g.Key)
            .Select(g => new ChannelProgress(g.Key.ToString(),
                g.Where(x => x.Status == NotificationStatus.Delivered).Sum(x => x.Count),
                g.Where(x => x.Status == NotificationStatus.Failed).Sum(x => x.Count),
                g.Where(x => x.Status == NotificationStatus.Pending).Sum(x => x.Count)))
            .ToArray();
        return new BroadcastProgress(total, delivered, total - delivered - pending, pending, acknowledged, channels);
    }

    public async Task<IReadOnlyList<BroadcastFailure>> GetBroadcastFailuresAsync(int broadcastId, int take, CancellationToken ct)
    {
        var rows = await (
                from notification in db.Notifications.AsNoTracking()
                join notice in db.Notices.AsNoTracking() on notification.NoticeId equals notice.Id
                where notice.BroadcastId == broadcastId && notification.Status == NotificationStatus.Failed
                orderby notice.Id, notification.Id
                select new
                {
                    NoticeId = notice.Id,
                    RecipientName = notice.Recipient.FullName,
                    notification.Channel,
                    notification.RecipientAddress,
                    notification.LastError
                })
            .Take(take)
            .ToListAsync(ct);
        return rows.Select(r => new BroadcastFailure(r.NoticeId, r.RecipientName, r.Channel.ToString(),
            r.RecipientAddress, r.LastError)).ToArray();
    }

    public void Add(Notice notice) => db.Notices.Add(notice);


    /// <summary>Ограничение «только свои» применяется в SQL, до загрузки связанных данных.</summary>
    private static IQueryable<Notice> Visible(IQueryable<Notice> query, int? ownerRecipientId) =>
        ownerRecipientId is { } owner ? query.Where(n => n.RecipientId == owner) : query;
}
