namespace Adimarip.Domain.Notices;

/// <summary>Статусы оповещения. Имена совпадают со значениями в БД (CK_Notices_Status_Allowed).</summary>
public enum NoticeStatus
{
    Created,
    Sent,
    Delivered,
    Read,
    Closed,
    Cancelled,
    Failed
}
