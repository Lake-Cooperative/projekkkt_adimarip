using Adimarip.Application.Abstractions;
using Adimarip.Domain.Notices;

namespace Adimarip.Application.Notices;

/// <summary>Параметры поиска в том виде, как они пришли из запроса.</summary>
public sealed record NoticeSearchInput(string? Status, int? OfficeId, string? Search, int? Page, int? PageSize);

/// <summary>Проверенные критерии поиска для репозитория.</summary>
public sealed record NoticeSearch(int? OwnerRecipientId, NoticeStatus? Status, int? OfficeId, string? Text, PageRequest Page);

public sealed record NoticeListItem(
    int Id, string Number, int RecipientId, string RecipientName, int OfficeId, string OfficeName,
    DateOnly CreatedAt, DateOnly ProcessingDeadline, string Reason, string? Comment, string CurrentStatus,
    int? BroadcastId, string? Severity);

public sealed record StatusEventDto(long Id, string Status, string? Comment, DateTimeOffset ChangedAt, string ChangedByUserId);
public sealed record AuditEventDto(long Id, string Action, string UserId, DateTimeOffset OccurredAt, string? Details);
public sealed record AttemptDto(string Status, DateTimeOffset AttemptedAt);
public sealed record NotificationDto(int Id, string Channel, string RecipientAddress, DateTimeOffset CreatedAt,
    string Status, string? LastError, IReadOnlyList<AttemptDto> Attempts);
public sealed record AppealDto(int Id, string Type, string Text, string Status, DateTimeOffset SubmittedAt);
public sealed record DocumentDto(int Id, string Name, string MimeType, string StorageUri, DateTimeOffset AddedAt);

public sealed record NoticeDetail(
    int Id, string Number, int RecipientId, string RecipientName, int OfficeId, string OfficeName,
    DateOnly CreatedAt, DateOnly ProcessingDeadline, string Reason, string? Comment, string CurrentStatus,
    int? BroadcastId, string? Severity,
    IReadOnlyList<string> AllowedTransitions, bool CanAcknowledge,
    IReadOnlyList<StatusEventDto> History, IReadOnlyList<AuditEventDto>? Audit,
    IReadOnlyList<NotificationDto> Notifications, IReadOnlyList<AppealDto> Appeals, IReadOnlyList<DocumentDto> Documents);

public sealed record CreateNoticeInput(string? Number, int RecipientId, int OfficeId,
    DateOnly? CreatedAt, DateOnly? ProcessingDeadline, string? Reason, string? Comment)
{
    public NoticeDraft ToDraft() => new(Number, RecipientId, OfficeId, CreatedAt, ProcessingDeadline, Reason, Comment);
}

public sealed record NoticeCreated(int Id, string Number, string CurrentStatus);

public sealed record ChangeStatusInput(string? Status, string? Comment);

public sealed record NoticeStatusChanged(int Id, string CurrentStatus);

public sealed record NotifyInput(string? Channel, string? RecipientAddress);

public sealed record NotificationSent(int Id, string Channel, bool Simulated, string Status);

public sealed record BulkNotifyInput(int[]? NoticeIds, string? Channel, string? RecipientAddress);

public sealed record BulkNotificationResult(bool Simulated, int Count, int Delivered, int Failed);

public sealed record AppealInput(string? Type, string? Text);

public sealed record AppealCreated(int Id, string Status);

public sealed record DocumentInput(string? Name, string? MimeType, string? StorageUri);

public sealed record DocumentCreated(int Id, string Name);
