using Adimarip.Domain.Common;
using Adimarip.Domain.Notices;

namespace Adimarip.Domain.Broadcasts;

/// <summary>Что ввёл автор массового оповещения.</summary>
public sealed record BroadcastDraft(
    string? Title,
    string? Message,
    string? Severity,
    IReadOnlyCollection<string>? Channels,
    int OfficeId);

/// <summary>
/// Агрегат «Массовое оповещение». Для каждого работника аудитории создаётся
/// отдельное оповещение реестра (Notice) с BroadcastId; здесь хранится сводка рассылки.
/// </summary>
public sealed class Broadcast
{
    public const int MaxTitleLength = 200;
    public const int MaxMessageLength = 4000;

    private Broadcast() { } // для EF Core

    public int Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string Message { get; private set; } = null!;
    public BroadcastSeverity Severity { get; private set; }
    public IReadOnlyList<DeliveryChannel> Channels { get; private set; } = [];
    public int OfficeId { get; private set; }
    public string AudienceDescription { get; private set; } = null!;
    public string CreatedByUserId { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public BroadcastStatus Status { get; private set; }
    public int TargetCount { get; private set; }
    public int DeliveredCount { get; private set; }
    public int FailedCount { get; private set; }

    public bool IsFinished => Status is BroadcastStatus.Completed or BroadcastStatus.CompletedWithErrors;

    /// <summary>Срок обработки оповещений рассылки: срочные — 1 день, остальные — 3 дня.</summary>
    public int ProcessingDays => Severity == BroadcastSeverity.Critical ? 1 : 3;

    public static Result<Broadcast> Create(BroadcastDraft draft, BroadcastAudience audience, int targetCount,
        string author, DateTimeOffset now, string code)
    {
        var title = TextRules.Clean(draft.Title);
        var message = TextRules.Clean(draft.Message);
        if (!TextRules.HasLength(title, 3, MaxTitleLength) || !TextRules.HasLength(message, 1, MaxMessageLength))
            return Error.Validation($"Заголовок: 3–{MaxTitleLength} символов, текст: 1–{MaxMessageLength} символов");
        if (!EnumNames.TryParse<BroadcastSeverity>(draft.Severity, out var severity))
            return Error.Validation("Важность: Info, Warning или Critical");

        var channels = new List<DeliveryChannel>();
        foreach (var raw in draft.Channels ?? [])
        {
            if (!EnumNames.TryParse<DeliveryChannel>(raw, out var channel))
                return Error.Validation($"Неизвестный канал доставки: {raw}");
            if (!channels.Contains(channel)) channels.Add(channel);
        }
        if (channels.Count == 0)
            return Error.Validation("Выберите хотя бы один канал доставки");
        if (targetCount < 1)
            return Error.Validation("В выбранной аудитории нет действующих адресатов");

        return new Broadcast
        {
            Code = code,
            Title = title,
            Message = message,
            Severity = severity,
            Channels = channels,
            OfficeId = draft.OfficeId,
            AudienceDescription = audience.Describe(),
            CreatedByUserId = author,
            CreatedAt = now,
            Status = BroadcastStatus.Queued,
            TargetCount = targetCount
        };
    }

    /// <summary>Номер оповещения реестра, созданного рассылкой для конкретного адресата.</summary>
    public string NoticeNumberFor(int recipientId) => $"{Code}-{recipientId}";

    public void StartDispatch(DateTimeOffset now)
    {
        if (IsFinished) return;
        Status = BroadcastStatus.Dispatching;
        StartedAt ??= now;
    }

    public void Complete(int delivered, int failed, DateTimeOffset now)
    {
        DeliveredCount = delivered;
        FailedCount = failed;
        CompletedAt = now;
        Status = failed == 0 ? BroadcastStatus.Completed : BroadcastStatus.CompletedWithErrors;
    }
}
