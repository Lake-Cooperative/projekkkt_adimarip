namespace Adimarip.Data.Entities;

public sealed class DeliveryAttempt
{
    public long Id { get; set; }
    public int NotificationId { get; set; }
    public string Status { get; set; } = "Delivered";
    public DateTimeOffset AttemptedAt { get; set; }

    public Notification Notification { get; set; } = null!;
}
