namespace Adimarip.Data.Entities;

public sealed class Notification
{
    public int Id { get; set; }
    public int NoticeId { get; set; }
    public string Channel { get; set; } = null!;
    public string RecipientAddress { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }

    public Notice Notice { get; set; } = null!;
    public ICollection<DeliveryAttempt> DeliveryAttempts { get; set; } = new List<DeliveryAttempt>();
}
