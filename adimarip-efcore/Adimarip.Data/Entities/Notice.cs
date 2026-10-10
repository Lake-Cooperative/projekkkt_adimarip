namespace Adimarip.Data.Entities;

public sealed class Notice
{
    public int Id { get; set; }
    public string Number { get; set; } = null!;
    public int RecipientId { get; set; }
    public int OfficeId { get; set; }
    public string AuthorUserId { get; set; } = null!;
    public DateOnly CreatedAt { get; set; }
    public DateOnly ProcessingDeadline { get; set; }
    public string Reason { get; set; } = null!;
    public string? Comment { get; set; }
    public string CurrentStatus { get; set; } = "Created";

    public Recipient Recipient { get; set; } = null!;
    public Office Office { get; set; } = null!;
    public ICollection<NoticeStatusHistory> StatusHistory { get; set; } = new List<NoticeStatusHistory>();
    public ICollection<AuditEvent> AuditEvents { get; set; } = new List<AuditEvent>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<Appeal> Appeals { get; set; } = new List<Appeal>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
