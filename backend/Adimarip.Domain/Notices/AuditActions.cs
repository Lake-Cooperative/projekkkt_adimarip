namespace Adimarip.Domain.Notices;

/// <summary>Коды событий аудита вместо «магических строк» по всему коду.</summary>
public static class AuditActions
{
    public const string NoticeCreated = "NoticeCreated";
    public const string NoticeSeeded = "NoticeSeeded";
    public const string StatusChanged = "StatusChanged";
    public const string DemoNotification = "DemoNotification";
    public const string BulkDemoNotification = "BulkDemoNotification";
    public const string AppealSubmitted = "AppealSubmitted";
    public const string DocumentLinked = "DocumentLinked";
    public const string BroadcastNoticeCreated = "BroadcastNoticeCreated";
    public const string BroadcastDelivery = "BroadcastDelivery";
    public const string Acknowledged = "Acknowledged";
}
