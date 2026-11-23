namespace Adimarip.Domain.Notices;

/// <summary>Уведомление по одному каналу. Хранит журнал попыток и итоговое состояние.</summary>
public sealed class Notification
{
    public const int MaxAddressLength = 320;
    public const int MaxErrorLength = 500;

    private readonly List<DeliveryAttempt> _deliveryAttempts = new();

    private Notification() { } // для EF Core

    internal Notification(DeliveryChannel channel, string address, DateTimeOffset createdAt)
    {
        Channel = channel;
        RecipientAddress = address;
        CreatedAt = createdAt;
        Status = NotificationStatus.Pending;
    }

    public int Id { get; private set; }
    public int NoticeId { get; private set; }
    public DeliveryChannel Channel { get; private set; }
    public string RecipientAddress { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public NotificationStatus Status { get; private set; }
    public string? LastError { get; private set; }
    public IReadOnlyCollection<DeliveryAttempt> DeliveryAttempts => _deliveryAttempts;

    public bool IsPending => Status == NotificationStatus.Pending;

    /// <summary>Фиксирует результат очередной попытки доставки.</summary>
    public void RecordAttempt(bool delivered, DateTimeOffset at, string? error = null)
    {
        if (!IsPending)
            throw new InvalidOperationException("Уведомление уже обработано");
        _deliveryAttempts.Add(new DeliveryAttempt(delivered ? DeliveryAttemptStatus.Delivered : DeliveryAttemptStatus.Failed, at));
        LastError = delivered ? null : Truncate(error);
    }

    /// <summary>Закрывает уведомление после всех попыток.</summary>
    public void Complete(bool delivered)
    {
        if (!IsPending) return;
        Status = delivered ? NotificationStatus.Delivered : NotificationStatus.Failed;
    }

    private static string? Truncate(string? text) =>
        text is { Length: > MaxErrorLength } ? text[..MaxErrorLength] : text;
}
