using Adimarip.Domain.Broadcasts;
using Adimarip.Domain.Common;
using Adimarip.Domain.Directory;

namespace Adimarip.Domain.Notices;

/// <summary>Данные для регистрации оповещения (то, что вводит пользователь).</summary>
public sealed record NoticeDraft(
    string? Number,
    int RecipientId,
    int OfficeId,
    DateOnly? CreatedAt,
    DateOnly? ProcessingDeadline,
    string? Reason,
    string? Comment);

/// <summary>
/// Агрегат «Оповещение». Все изменения (статус, уведомления, обращения, документы)
/// проходят через методы агрегата, поэтому история и аудит не могут «забыться».
/// </summary>
public sealed class Notice
{
    public const int MaxBackdatingDays = 3;
    public const string SystemActor = "system";

    private readonly List<NoticeStatusHistory> _statusHistory = new();
    private readonly List<AuditEvent> _auditEvents = new();
    private readonly List<Notification> _notifications = new();
    private readonly List<Appeal> _appeals = new();
    private readonly List<Document> _documents = new();

    private Notice() { } // для EF Core

    public int Id { get; private set; }
    public string Number { get; private set; } = null!;
    public int RecipientId { get; private set; }
    public int OfficeId { get; private set; }
    public int? BroadcastId { get; private set; }
    public string AuthorUserId { get; private set; } = null!;
    public DateOnly CreatedAt { get; private set; }
    public DateOnly ProcessingDeadline { get; private set; }
    public string Reason { get; private set; } = null!;
    public string? Comment { get; private set; }
    public NoticeStatus CurrentStatus { get; private set; }

    public Recipient Recipient { get; private set; } = null!;
    public Office Office { get; private set; } = null!;
    public Broadcast? Broadcast { get; private set; }
    public IReadOnlyCollection<NoticeStatusHistory> StatusHistory => _statusHistory;
    public IReadOnlyCollection<AuditEvent> AuditEvents => _auditEvents;
    public IReadOnlyCollection<Notification> Notifications => _notifications;
    public IReadOnlyCollection<Appeal> Appeals => _appeals;
    public IReadOnlyCollection<Document> Documents => _documents;

    public IReadOnlyList<NoticeStatus> AllowedTransitions => NoticeLifecycle.AllowedFrom(CurrentStatus);

    /// <summary>Фабричный метод: проверяет инварианты и создаёт начальную историю и аудит.</summary>
    public static Result<Notice> Register(NoticeDraft draft, string author, DateTimeOffset now,
        int? broadcastId = null, string auditAction = AuditActions.NoticeCreated)
    {
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var created = draft.CreatedAt ?? today;
        if (created < today.AddDays(-MaxBackdatingDays))
            return Error.Validation("Дата создания не может быть ранее чем три дня назад");

        var number = TextRules.Clean(draft.Number);
        var reason = TextRules.Clean(draft.Reason);
        var comment = TextRules.CleanOptional(draft.Comment);
        if (!TextRules.HasLength(number, 3, 100) || !TextRules.HasLength(reason, 1, 1000) ||
            !TextRules.FitsOptional(comment, 4000) || draft.ProcessingDeadline is not { } deadline || deadline < created)
            return Error.Validation("Проверьте номер, причину и срок обработки");

        var notice = new Notice
        {
            Number = number,
            RecipientId = draft.RecipientId,
            OfficeId = draft.OfficeId,
            BroadcastId = broadcastId,
            AuthorUserId = author,
            CreatedAt = created,
            ProcessingDeadline = deadline,
            Reason = reason,
            Comment = comment,
            CurrentStatus = NoticeStatus.Created
        };
        notice._statusHistory.Add(new NoticeStatusHistory(NoticeStatus.Created, author, null, now));
        notice._auditEvents.Add(new AuditEvent(auditAction, author, now));
        return notice;
    }

    /// <summary>Только для демонстрационных данных: запись сразу в нужном статусе, без проверок дат.</summary>
    public static Notice Seed(NoticeDraft draft, NoticeStatus status, DateTimeOffset now)
    {
        var notice = new Notice
        {
            Number = TextRules.Clean(draft.Number),
            RecipientId = draft.RecipientId,
            OfficeId = draft.OfficeId,
            AuthorUserId = "seed",
            CreatedAt = draft.CreatedAt ?? DateOnly.FromDateTime(now.UtcDateTime),
            ProcessingDeadline = draft.ProcessingDeadline ?? DateOnly.FromDateTime(now.UtcDateTime),
            Reason = TextRules.Clean(draft.Reason),
            Comment = TextRules.CleanOptional(draft.Comment),
            CurrentStatus = status
        };
        notice._statusHistory.Add(new NoticeStatusHistory(status, "seed", null, now));
        notice._auditEvents.Add(new AuditEvent(AuditActions.NoticeSeeded, "seed", now));
        return notice;
    }

    public Result ChangeStatus(NoticeStatus target, string actor, string? comment, DateTimeOffset now)
    {
        var cleanComment = TextRules.CleanOptional(comment);
        if (!TextRules.FitsOptional(cleanComment, 4000))
            return Error.Validation("Слишком длинный комментарий");
        if (!NoticeLifecycle.CanTransition(CurrentStatus, target))
            return Error.Conflict($"Переход {CurrentStatus} → {target} не разрешён");

        CurrentStatus = target;
        _statusHistory.Add(new NoticeStatusHistory(target, actor, cleanComment, now));
        _auditEvents.Add(new AuditEvent(AuditActions.StatusChanged, actor, now, $"Status: {target}"));
        return Result.Success();
    }

    private static readonly NoticeStatus[] AcknowledgementPath = [NoticeStatus.Sent, NoticeStatus.Delivered, NoticeStatus.Read];

    /// <summary>Адресат подтверждает, что ознакомился с оповещением: статус доводится до Read по разрешённым переходам.</summary>
    public Result Acknowledge(string actor, DateTimeOffset now)
    {
        if (!NoticeLifecycle.AwaitsAcknowledgement(CurrentStatus))
            return Error.Conflict("Оповещение уже подтверждено или закрыто");

        var remainingSteps = AcknowledgementPath.Skip(Array.IndexOf(AcknowledgementPath, CurrentStatus) + 1);
        foreach (var step in remainingSteps)
        {
            var comment = step == NoticeStatus.Read ? "Получение подтверждено адресатом" : null;
            var changed = ChangeStatus(step, actor, comment, now);
            if (changed.IsFailure) return changed;
        }
        _auditEvents.Add(new AuditEvent(AuditActions.Acknowledged, actor, now));
        return Result.Success();
    }

    /// <summary>Ставит уведомление в очередь доставки (результат записывает отправитель).</summary>
    public Result<Notification> QueueNotification(DeliveryChannel channel, string? address, DateTimeOffset now)
    {
        var cleanAddress = TextRules.Clean(address);
        if (!TextRules.HasLength(cleanAddress, 2, Notification.MaxAddressLength))
            return Error.Validation("Недопустимый канал или адрес доставки");
        var notification = new Notification(channel, cleanAddress, now);
        _notifications.Add(notification);
        return notification;
    }

    public void Audit(string action, string actor, DateTimeOffset now, string? details = null) =>
        _auditEvents.Add(new AuditEvent(action, actor, now, details));

    public Result<Appeal> FileAppeal(string? type, string? text, string actor, DateTimeOffset now)
    {
        var kind = TextRules.Clean(type);
        var body = TextRules.Clean(text);
        if (!TextRules.HasLength(kind, 1, 100) || !TextRules.HasLength(body, 1, 4000))
            return Error.Validation("Укажите тип и текст обращения");
        var appeal = new Appeal(kind, body, now);
        _appeals.Add(appeal);
        _auditEvents.Add(new AuditEvent(AuditActions.AppealSubmitted, actor, now));
        return appeal;
    }

    public Result<Document> AttachDocument(string? name, string? mimeType, string? storageUri, string actor, DateTimeOffset now)
    {
        var cleanName = TextRules.Clean(name);
        var mime = TextRules.Clean(mimeType);
        var uri = TextRules.Clean(storageUri);
        if (!TextRules.HasLength(cleanName, 1, 255) || !TextRules.HasLength(mime, 3, 150) || !TextRules.HasLength(uri, 6, 2048) ||
            !Uri.TryCreate(uri, UriKind.Absolute, out var parsed) || parsed.Scheme != Uri.UriSchemeHttps)
            return Error.Validation("Проверьте имя, MIME-тип и HTTPS-ссылку на документ");
        var document = new Document(cleanName, mime, uri, now);
        _documents.Add(document);
        _auditEvents.Add(new AuditEvent(AuditActions.DocumentLinked, actor, now));
        return document;
    }

    /// <summary>
    /// Переводит оповещение массовой рассылки по итогам доставки:
    /// хотя бы одно уведомление доставлено → Delivered, иначе → Failed.
    /// </summary>
    public Result ApplyDeliveryOutcome(DateTimeOffset now)
    {
        // Адресат мог подтвердить получение раньше, чем завершилась рассылка, — тогда статус не трогаем.
        if (CurrentStatus != NoticeStatus.Created) return Result.Success();
        if (_notifications.Any(n => n.IsPending))
            return Error.Conflict("Доставка ещё не завершена");

        var sent = ChangeStatus(NoticeStatus.Sent, SystemActor, "Передано в каналы доставки", now);
        if (sent.IsFailure) return sent;
        var delivered = _notifications.Any(n => n.Status == NotificationStatus.Delivered);
        return delivered
            ? ChangeStatus(NoticeStatus.Delivered, SystemActor, null, now)
            : ChangeStatus(NoticeStatus.Failed, SystemActor, "Ни один канал не доставил сообщение", now);
    }
}
