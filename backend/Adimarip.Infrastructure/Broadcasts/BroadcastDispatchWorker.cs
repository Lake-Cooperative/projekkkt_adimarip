using System.Diagnostics;
using Adimarip.Application.Abstractions;
using Adimarip.Application.Broadcasts;
using Adimarip.Application.Delivery;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Adimarip.Infrastructure.Broadcasts;

/// <summary>
/// Фоновый обработчик рассылок. Несколько потребителей очереди работают параллельно,
/// поэтому срочное оповещение не ждёт окончания большой плановой рассылки.
/// </summary>
internal sealed class BroadcastDispatchWorker(
    IServiceScopeFactory scopes,
    IBroadcastQueue queue,
    DeliveryOptions options,
    ILogger<BroadcastDispatchWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.SimulationEnabled)
        {
            logger.LogInformation("Broadcast delivery is disabled: no notification provider is configured");
            return;
        }
        await RequeueUnfinishedAsync(stoppingToken);
        var consumers = Enumerable.Range(1, Math.Max(1, options.Consumers)).Select(n => ConsumeAsync(n, stoppingToken));
        await Task.WhenAll(consumers);
    }

    private async Task RequeueUnfinishedAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IBroadcastRepository>();
            foreach (var (id, severity) in await repository.GetUnfinishedAsync(ct))
            {
                logger.LogInformation("Resuming unfinished broadcast {BroadcastId}", id);
                queue.Enqueue(id, severity);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Сбой восстановления не должен останавливать API: новые рассылки продолжат обрабатываться.
            logger.LogError(ex, "Could not resume unfinished broadcasts");
        }
    }

    private async Task ConsumeAsync(int consumer, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            int broadcastId;
            try
            {
                broadcastId = await queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            var timer = Stopwatch.StartNew();
            try
            {
                using var scope = scopes.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<BroadcastDispatcher>();
                await dispatcher.DispatchAsync(broadcastId, stoppingToken);
                logger.LogInformation("Broadcast {BroadcastId} dispatched by consumer {Consumer} in {Elapsed} ms",
                    broadcastId, consumer, timer.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return; // рассылка будет продолжена при следующем запуске
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Broadcast {BroadcastId} failed; it will be resumed after restart", broadcastId);
            }
        }
    }
}
