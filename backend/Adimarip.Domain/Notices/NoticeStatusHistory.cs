namespace Adimarip.Domain.Notices;

public sealed class NoticeStatusHistory
{
    private NoticeStatusHistory() { } // для EF Core

    internal NoticeStatusHistory(NoticeStatus status, string changedBy, string? comment, DateTimeOffset changedAt)
    {
        Status = status;
        ChangedByUserId = changedBy;
        Comment = comment;
        ChangedAt = changedAt;
    }

    public long Id { get; private set; }
    public int NoticeId { get; private set; }
    public NoticeStatus Status { get; private set; }
    public string? Comment { get; private set; }
    public DateTimeOffset ChangedAt { get; private set; }
    public string ChangedByUserId { get; private set; } = null!;
}
