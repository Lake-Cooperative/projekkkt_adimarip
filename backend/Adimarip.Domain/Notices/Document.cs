namespace Adimarip.Domain.Notices;

/// <summary>Метаданные сопроводительного документа: сам файл не хранится, только HTTPS-ссылка.</summary>
public sealed class Document
{
    private Document() { } // для EF Core

    internal Document(string name, string mimeType, string storageUri, DateTimeOffset addedAt)
    {
        Name = name;
        MimeType = mimeType;
        StorageUri = storageUri;
        AddedAt = addedAt;
    }

    public int Id { get; private set; }
    public int NoticeId { get; private set; }
    public string Name { get; private set; } = null!;
    public string MimeType { get; private set; } = null!;
    public string StorageUri { get; private set; } = null!;
    public DateTimeOffset AddedAt { get; private set; }
}
