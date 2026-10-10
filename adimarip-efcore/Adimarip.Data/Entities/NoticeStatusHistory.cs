namespace Adimarip.Data.Entities;

public sealed class NoticeStatusHistory
{
    public long Id { get; set; }
    public int NoticeId { get; set; }
    public string Status { get; set; } = null!;
    public string? Comment { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
    public string ChangedByUserId { get; set; } = null!;

    public Notice Notice { get; set; } = null!;
}
