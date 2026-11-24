using Adimarip.Application.Broadcasts;
using Adimarip.Domain.Broadcasts;

namespace Adimarip.Infrastructure.Broadcasts;

/// <summary>
/// Очередь с приоритетом в памяти процесса. Потеря очереди при перезапуске не страшна:
/// источник истины — таблица Broadcasts, и обработчик при старте заново ставит
/// незавершённые рассылки в очередь.
/// </summary>
internal sealed class InMemoryBroadcastQueue : IBroadcastQueue
{
    private readonly object _gate = new();
    private readonly PriorityQueue<int, (int Priority, long Sequence)> _queue = new();
    private readonly HashSet<int> _queued = new();
    private readonly SemaphoreSlim _available = new(0);
    private long _sequence;

    public void Enqueue(int broadcastId, BroadcastSeverity severity)
    {
        lock (_gate)
        {
            if (!_queued.Add(broadcastId)) return; // уже в очереди
            _queue.Enqueue(broadcastId, (PriorityOf(severity), _sequence++));
        }
        _available.Release();
    }

    public async ValueTask<int> DequeueAsync(CancellationToken ct)
    {
        await _available.WaitAsync(ct);
        lock (_gate)
        {
            var id = _queue.Dequeue();
            _queued.Remove(id);
            return id;
        }
    }

    private static int PriorityOf(BroadcastSeverity severity) => severity switch
    {
        BroadcastSeverity.Critical => 0,
        BroadcastSeverity.Warning => 1,
        _ => 2
    };
}
