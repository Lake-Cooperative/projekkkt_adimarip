namespace Adimarip.Domain.Notices;

public sealed class Appeal
{
    public const string SubmittedStatus = "Submitted";

    private Appeal() { } // для EF Core

    internal Appeal(string type, string text, DateTimeOffset submittedAt)
    {
        Type = type;
        Text = text;
        Status = SubmittedStatus;
        SubmittedAt = submittedAt;
    }

    public int Id { get; private set; }
    public int NoticeId { get; private set; }
    public string Type { get; private set; } = null!;
    public string Text { get; private set; } = null!;
    public string Status { get; private set; } = SubmittedStatus;
    public DateTimeOffset SubmittedAt { get; private set; }
}
