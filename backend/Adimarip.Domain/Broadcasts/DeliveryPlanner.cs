using Adimarip.Domain.Notices;

namespace Adimarip.Domain.Broadcasts;

/// <summary>Контакты адресата, достаточные для планирования доставки.</summary>
public sealed record RecipientContacts(int RecipientId, string FullName, string? Email, string? Phone);

/// <summary>Куда и по какому каналу отправить сообщение конкретному адресату.</summary>
public sealed record PlannedDelivery(DeliveryChannel Channel, string Address);

/// <summary>
/// Планировщик доставки. Для каждого канала — своя стратегия получения адреса
/// (Strategy), а если ни один выбранный канал недоступен, срабатывает резервный
/// канал «Портал» (fallback), чтобы ни один работник не остался без оповещения.
/// </summary>
public static class DeliveryPlanner
{
    public const DeliveryChannel FallbackChannel = DeliveryChannel.Portal;

    private static readonly IReadOnlyDictionary<DeliveryChannel, Func<RecipientContacts, string?>> AddressResolvers =
        new Dictionary<DeliveryChannel, Func<RecipientContacts, string?>>
        {
            [DeliveryChannel.Email] = c => c.Email,
            [DeliveryChannel.Sms] = c => c.Phone,
            [DeliveryChannel.Max] = c => c.Phone,
            [DeliveryChannel.Portal] = c => $"portal:{c.RecipientId}"
        };

    public static IReadOnlyList<PlannedDelivery> Plan(RecipientContacts contacts,
        IReadOnlyCollection<DeliveryChannel> requested, BroadcastSeverity severity)
    {
        // Критическое оповещение идёт по всем каналам, где у работника есть контакт.
        var channels = severity == BroadcastSeverity.Critical ? DeliveryChannels.All : requested;
        var plan = channels
            .Distinct()
            .Select(channel => (channel, address: AddressResolvers[channel](contacts)))
            .Where(x => !string.IsNullOrWhiteSpace(x.address))
            .Select(x => new PlannedDelivery(x.channel, x.address!.Trim()))
            .ToList();

        if (plan.Count == 0)
            plan.Add(new PlannedDelivery(FallbackChannel, AddressResolvers[FallbackChannel](contacts)!));
        return plan;
    }
}
