namespace Adimarip.Domain.Notices;

/// <summary>Каналы доставки. Имена совпадают с CK_Notifications_Channel_Allowed.</summary>
public enum DeliveryChannel
{
    Email,
    Sms,
    Portal,
    Max
}

public static class DeliveryChannels
{
    public static IReadOnlyList<DeliveryChannel> All { get; } = Enum.GetValues<DeliveryChannel>();
}
