namespace Adimarip.Domain.Notices;

public sealed class AuditEvent
{
    private AuditEvent() { } // для EF Core

    internal AuditEvent(string action, string userId, DateTimeOffset occurredAt, string? details = null)
    {
        Action = action;
        UserId = userId;
        OccurredAt = occurredAt;
        Details = details;
    }

    public long Id { get; private set; }
    public int NoticeId { get; private set; }
    public string Action { get; private set; } = null!;
    public string UserId { get; private set; } = null!;
    public DateTimeOffset OccurredAt { get; private set; }
    public string? ObjectType { get; private set; }
    public string? ObjectId { get; private set; }
    public string? Details { get; private set; }
}
