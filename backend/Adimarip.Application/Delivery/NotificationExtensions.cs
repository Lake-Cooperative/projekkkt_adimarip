using Adimarip.Domain.Notices;

namespace Adimarip.Application.Delivery;

public static class NotificationExtensions
{
    /// <summary>Переносит журнал попыток в доменное уведомление и закрывает его.</summary>
    public static void Apply(this Notification notification, DeliveryReport report)
    {
        foreach (var attempt in report.Attempts)
            notification.RecordAttempt(attempt.Delivered, attempt.At, attempt.Error);
        notification.Complete(report.Delivered);
    }
}
