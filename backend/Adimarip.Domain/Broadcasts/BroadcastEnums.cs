namespace Adimarip.Domain.Broadcasts;

/// <summary>Важность массового оповещения. Critical обрабатывается вне очереди и по всем доступным каналам.</summary>
public enum BroadcastSeverity
{
    Info,
    Warning,
    Critical
}

public enum BroadcastStatus
{
    Queued,
    Dispatching,
    Completed,
    CompletedWithErrors
}

/// <summary>Кого оповещаем.</summary>
public enum AudienceKind
{
    All,
    Offices,
    Recipients
}
