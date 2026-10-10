namespace Adimarip.Data.Entities;

public sealed class Appeal
{
    public int Id { get; set; }
    public int NoticeId { get; set; }
    public string Type { get; set; } = null!;
    public string Text { get; set; } = null!;
    public string Status { get; set; } = "Submitted";
    public DateTimeOffset SubmittedAt { get; set; }

    public Notice Notice { get; set; } = null!;
}
