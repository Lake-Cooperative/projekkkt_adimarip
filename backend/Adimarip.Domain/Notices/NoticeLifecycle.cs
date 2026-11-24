namespace Adimarip.Domain.Notices;

/// <summary>
/// Конечный автомат жизненного цикла оповещения (табличная реализация паттерна State).
/// Единственный источник правды о переходах: им пользуются домен, API и интерфейс
/// (список переходов отдаётся в карточке, а не дублируется во frontend).
/// </summary>
public static class NoticeLifecycle
{
    private static readonly IReadOnlyDictionary<NoticeStatus, NoticeStatus[]> Transitions =
        new Dictionary<NoticeStatus, NoticeStatus[]>
        {
            [NoticeStatus.Created] = [NoticeStatus.Sent, NoticeStatus.Cancelled],
            [NoticeStatus.Sent] = [NoticeStatus.Delivered, NoticeStatus.Failed, NoticeStatus.Cancelled],
            [NoticeStatus.Failed] = [NoticeStatus.Sent, NoticeStatus.Cancelled],
            [NoticeStatus.Delivered] = [NoticeStatus.Read, NoticeStatus.Closed],
            [NoticeStatus.Read] = [NoticeStatus.Closed],
            [NoticeStatus.Closed] = [],
            [NoticeStatus.Cancelled] = []
        };

    public static IReadOnlyList<NoticeStatus> AllowedFrom(NoticeStatus current) =>
        Transitions.TryGetValue(current, out var next) ? next : [];

    public static bool CanTransition(NoticeStatus from, NoticeStatus to) => AllowedFrom(from).Contains(to);

    /// <summary>Статусы, в которых адресат ещё не подтвердил получение.</summary>
    public static bool AwaitsAcknowledgement(NoticeStatus status) =>
        status is NoticeStatus.Created or NoticeStatus.Sent or NoticeStatus.Delivered;
}
