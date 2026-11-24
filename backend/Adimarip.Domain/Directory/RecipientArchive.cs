namespace Adimarip.Domain.Directory;

/// <summary>Запись о списании адресата. Сам адресат и его история не удаляются.</summary>
public sealed class RecipientArchive
{
    private RecipientArchive() { } // для EF Core

    internal RecipientArchive(int recipientId, string reason, string archivedBy, DateTimeOffset archivedAt)
    {
        RecipientId = recipientId;
        Reason = reason;
        ArchivedByUserId = archivedBy;
        ArchivedAt = archivedAt;
    }

    public int RecipientId { get; private set; }
    public DateTimeOffset ArchivedAt { get; private set; }
    public string ArchivedByUserId { get; private set; } = null!;
    public string Reason { get; private set; } = null!;
}
