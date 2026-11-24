namespace Adimarip.Domain.Notices;

/// <summary>Состояние уведомления в очереди доставки.</summary>
public enum NotificationStatus
{
    Pending,
    Delivered,
    Failed
}

/// <summary>Итог одной попытки (подмножество значений CK_DeliveryAttempts_Status_Allowed).</summary>
public enum DeliveryAttemptStatus
{
    Delivered,
    Failed
}
