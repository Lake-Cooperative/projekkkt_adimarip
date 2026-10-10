namespace Adimarip.Data.Entities;

public sealed class Document
{
    public int Id { get; set; }
    public int NoticeId { get; set; }
    public string Name { get; set; } = null!;
    public string MimeType { get; set; } = null!;
    public string StorageUri { get; set; } = null!;
    public DateTimeOffset AddedAt { get; set; }

    public Notice Notice { get; set; } = null!;
}
