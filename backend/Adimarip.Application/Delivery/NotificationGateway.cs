using Adimarip.Application.Abstractions;
using Adimarip.Domain.Notices;

namespace Adimarip.Application.Delivery;

/// <summary>
/// Выбирает стратегию канала (реестр стратегий, собранный DI) и повторяет
/// временные сбои с экспоненциальной задержкой. Постоянные ошибки (неверный
/// адрес) не повторяются — это бессмысленно и только замедлит рассылку.
/// </summary>
public sealed class NotificationGateway : INotificationGateway
{
    private readonly IReadOnlyDictionary<DeliveryChannel, IChannelSender> _senders;
    private readonly DeliveryOptions _options;
    private readonly IClock _clock;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;

    public NotificationGateway(IEnumerable<IChannelSender> senders, DeliveryOptions options, IClock clock,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        _senders = senders.ToDictionary(s => s.Channel);
        _options = options;
        _clock = clock;
        _delay = delay ?? Task.Delay;
    }

    public bool IsConfigured => _options.SimulationEnabled && _senders.Count > 0;

    public async Task<DeliveryReport> DeliverAsync(OutgoingMessage message, CancellationToken ct)
    {
        if (!_senders.TryGetValue(message.Channel, out var sender))
            return new DeliveryReport([new AttemptRecord(false, _clock.UtcNow, $"Канал {message.Channel} не подключён")], Simulated: true);

        var attempts = new List<AttemptRecord>();
        var maxAttempts = Math.Max(1, _options.MaxAttempts);
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var outcome = await SafeSendAsync(sender, message, ct);
            attempts.Add(new AttemptRecord(outcome.Delivered, _clock.UtcNow, outcome.Error));
            if (outcome.Delivered || !outcome.Transient || attempt == maxAttempts) break;

            // Срочные сообщения повторяются без нарастания задержки.
            var factor = message.Urgent ? 1 : 1 << (attempt - 1);
            await _delay(TimeSpan.FromMilliseconds(_options.RetryBaseDelayMs * factor), ct);
        }
        return new DeliveryReport(attempts, sender.IsSimulated);
    }

    private static async Task<SendOutcome> SafeSendAsync(IChannelSender sender, OutgoingMessage message, CancellationToken ct)
    {
        try
        {
            return await sender.SendAsync(message, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Сбой провайдера не должен ронять всю рассылку: считаем его временным.
            return SendOutcome.Temporary(ex.Message);
        }
    }
}
