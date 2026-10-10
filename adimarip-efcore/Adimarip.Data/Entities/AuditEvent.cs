namespace Adimarip.Data.Entities;

public sealed class AuditEvent
{
    public long Id { get; set; }
    public int NoticeId { get; set; }
    public string Action { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public DateTimeOffset OccurredAt { get; set; }
    public string? ObjectType { get; set; }
    public string? ObjectId { get; set; }
    public string? Details { get; set; }

    public Notice Notice { get; set; } = null!;
}
