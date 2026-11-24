using Adimarip.Application.Abstractions;
using Adimarip.Application.Delivery;
using Adimarip.Domain.Broadcasts;
using Adimarip.Domain.Notices;

namespace Adimarip.Application.Broadcasts;

/// <summary>
/// Доставка рассылки порциями: внутри порции сообщения уходят параллельно
/// (до DeliveryOptions.MaxParallelism одновременно), а результат порции
/// сохраняется одной транзакцией. Обработка идемпотентна: после перезапуска
/// продолжается с первого оповещения, где ещё остались неотправленные уведомления.
/// </summary>
public sealed class BroadcastDispatcher(
    IBroadcastRepository broadcasts,
    INoticeRepository notices,
    INotificationGateway gateway,
    IUnitOfWork unitOfWork,
    DeliveryOptions options,
    IClock clock)
{
    private const int MaxConflictRetries = 3;

    public async Task DispatchAsync(int broadcastId, CancellationToken ct)
    {
        var broadcast = await broadcasts.GetForUpdateAsync(broadcastId, ct);
        if (broadcast is null || broadcast.IsFinished) return;

        broadcast.StartDispatch(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(ct);

        var processed = new HashSet<int>();
        var conflicts = 0;
        while (!ct.IsCancellationRequested)
        {
            var batch = await notices.GetUndeliveredBroadcastBatchAsync(broadcastId, Math.Max(1, options.BatchSize), ct);
            // Защита от зацикливания: порция без новых записей означает, что прогресса нет.
            if (batch.Count == 0 || batch.All(n => processed.Contains(n.Id))) break;

            // 1. Доставка и журнал попыток. Сохраняются только строки уведомлений,
            //    поэтому повторной отправки после сбоя следующего шага не будет.
            await DeliverBatchAsync(broadcast, batch, ct);
            await unitOfWork.SaveChangesAsync(ct);

            // 2. Статусы оповещений по итогам доставки.
            foreach (var notice in batch)
                notice.ApplyDeliveryOutcome(clock.UtcNow);
            if (await TrySaveAsync(ct))
            {
                processed.UnionWith(batch.Select(n => n.Id));
            }
            else if (++conflicts > MaxConflictRetries)
            {
                break;
            }
        }

        ct.ThrowIfCancellationRequested();
        await CompleteAsync(broadcastId, ct);
    }

    private async Task CompleteAsync(int broadcastId, CancellationToken ct)
    {
        // Перечитываем: после конфликта трекер изменений мог быть очищен.
        var broadcast = await broadcasts.GetForUpdateAsync(broadcastId, ct);
        if (broadcast is null) return;
        var progress = await notices.GetBroadcastProgressAsync(broadcastId, ct);
        broadcast.Complete(progress.Delivered, progress.Failed, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task DeliverBatchAsync(Broadcast broadcast, IReadOnlyList<Notice> batch, CancellationToken ct)
    {
        var jobs = batch.SelectMany(notice => notice.Notifications.Where(n => n.IsPending)).ToArray();
        var reports = new DeliveryReport[jobs.Length];
        var subject = BroadcastService.ReasonFor(broadcast);
        var urgent = broadcast.Severity == BroadcastSeverity.Critical;

        await Parallel.ForEachAsync(Enumerable.Range(0, jobs.Length),
            new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, options.MaxParallelism), CancellationToken = ct },
            async (index, token) =>
            {
                var job = jobs[index];
                reports[index] = await gateway.DeliverAsync(
                    new OutgoingMessage(job.Channel, job.RecipientAddress, subject, broadcast.Message, urgent), token);
            });

        // DbContext не потокобезопасен, поэтому результаты применяются уже последовательно.
        for (var i = 0; i < jobs.Length; i++)
            jobs[i].Apply(reports[i]);
    }

    private async Task<bool> TrySaveAsync(CancellationToken ct)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
            return true;
        }
        catch (PersistenceConflictException)
        {
            // Работник подтвердил получение одновременно с рассылкой: его статус важнее.
            // Сбрасываем изменения — следующая порция перечитает актуальные данные.
            unitOfWork.DiscardChanges();
            return false;
        }
    }
}
