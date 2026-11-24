namespace Adimarip.Domain.Notices;

public sealed class DeliveryAttempt
{
    private DeliveryAttempt() { } // для EF Core

    internal DeliveryAttempt(DeliveryAttemptStatus status, DateTimeOffset attemptedAt)
    {
        Status = status;
        AttemptedAt = attemptedAt;
    }

    public long Id { get; private set; }
    public int NotificationId { get; private set; }
    public DeliveryAttemptStatus Status { get; private set; }
    public DateTimeOffset AttemptedAt { get; private set; }
}
