using Adimarip.Application.Access;
using Adimarip.Domain.Access;
using Adimarip.Domain.Notices;

namespace Adimarip.Application.Notices;

/// <summary>Преобразует агрегат в карточку с учётом прав: история, аудит и переходы видны не всем.</summary>
internal static class NoticeDetailMapper
{
    public static NoticeDetail Map(Notice notice, UserAccess access) => new(
        notice.Id, notice.Number, notice.RecipientId, notice.Recipient.FullName, notice.OfficeId, notice.Office.Name,
        notice.CreatedAt, notice.ProcessingDeadline, notice.Reason, notice.Comment, notice.CurrentStatus.ToString(),
        notice.BroadcastId, notice.Broadcast?.Severity.ToString(),
        AllowedTransitions: access.Has(Permission.NoticesChangeStatus)
            ? notice.AllowedTransitions.Select(s => s.ToString()).ToArray()
            : [],
        CanAcknowledge: CanAcknowledge(notice, access),
        History: access.Has(Permission.HistoryView) ? History(notice) : [],
        Audit: access.Has(Permission.AuditView) ? Audit(notice) : null,
        Notifications: Notifications(notice),
        Appeals: notice.Appeals.OrderByDescending(a => a.SubmittedAt)
            .Select(a => new AppealDto(a.Id, a.Type, a.Text, a.Status, a.SubmittedAt)).ToArray(),
        Documents: notice.Documents.OrderByDescending(d => d.AddedAt)
            .Select(d => new DocumentDto(d.Id, d.Name, d.MimeType, d.StorageUri, d.AddedAt)).ToArray());

    private static bool CanAcknowledge(Notice notice, UserAccess access) =>
        access.Has(Permission.NoticesAcknowledgeOwn) &&
        access.RecipientId == notice.RecipientId &&
        NoticeLifecycle.AwaitsAcknowledgement(notice.CurrentStatus);

    private static StatusEventDto[] History(Notice notice) =>
        notice.StatusHistory.OrderByDescending(h => h.ChangedAt)
            .Select(h => new StatusEventDto(h.Id, h.Status.ToString(), h.Comment, h.ChangedAt, h.ChangedByUserId))
            .ToArray();

    private static AuditEventDto[] Audit(Notice notice) =>
        notice.AuditEvents.OrderByDescending(a => a.OccurredAt)
            .Select(a => new AuditEventDto(a.Id, a.Action, a.UserId, a.OccurredAt, a.Details))
            .ToArray();

    private static NotificationDto[] Notifications(Notice notice) =>
        notice.Notifications.OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto(n.Id, n.Channel.ToString(), n.RecipientAddress, n.CreatedAt,
                n.Status.ToString(), n.LastError,
                n.DeliveryAttempts.OrderByDescending(a => a.AttemptedAt)
                    .Select(a => new AttemptDto(a.Status.ToString(), a.AttemptedAt)).ToArray()))
            .ToArray();
}
