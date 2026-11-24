using Adimarip.Domain.Broadcasts;

namespace Adimarip.Application.Broadcasts;

/// <summary>
/// Очередь рассылок для фонового обработчика (Producer–Consumer).
/// Срочные (Critical) рассылки извлекаются раньше остальных.
/// </summary>
public interface IBroadcastQueue
{
    void Enqueue(int broadcastId, BroadcastSeverity severity);
    ValueTask<int> DequeueAsync(CancellationToken ct);
}
