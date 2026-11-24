using Adimarip.Application.Abstractions;
using Adimarip.Application.Access;
using Adimarip.Application.Delivery;
using Adimarip.Domain.Common;
using Adimarip.Domain.Notices;

namespace Adimarip.Application.Notices;

/// <summary>Уведомления по отдельным оповещениям (по одному и пакетом до 50 записей).</summary>
public sealed class NotificationService(
    INoticeRepository notices,
    INotificationGateway gateway,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public const int MaxBulkSize = 50;
    private const string NotConfiguredMessage = "Провайдер доставки не настроен: отправка доступна только в демо-режиме";

    public async Task<Result<NotificationSent>> SendAsync(UserAccess access, int noticeId, NotifyInput input, CancellationToken ct)
    {
        if (!gateway.IsConfigured)
            return Error.NotConfigured(NotConfiguredMessage);
        if (!EnumNames.TryParse<DeliveryChannel>(input.Channel, out var channel))
            return Error.Validation("Недопустимый канал или адрес доставки");

        var notice = await notices.GetForUpdateAsync(noticeId, ownerRecipientId: null, ct);
        if (notice is null) return Error.NotFound("Оповещение не найдено");

        var sent = await DeliverAsync(notice, channel, input.RecipientAddress, AuditActions.DemoNotification, access, ct);
        if (sent.IsFailure) return sent.Error!;
        await unitOfWork.SaveChangesAsync(ct);
        var notification = sent.Value;
        return new NotificationSent(notification.Id, channel.ToString(), Simulated: true, notification.Status.ToString());
    }

    public async Task<Result<BulkNotificationResult>> SendBulkAsync(UserAccess access, BulkNotifyInput input, CancellationToken ct)
    {
        if (!gateway.IsConfigured)
            return Error.NotConfigured(NotConfiguredMessage);
        var ids = input.NoticeIds?.Distinct().ToArray() ?? [];
        var address = TextRules.Clean(input.RecipientAddress);
        var idsAreValid = ids.Length is >= 1 and <= MaxBulkSize && ids.All(id => id > 0);
        if (!idsAreValid || !EnumNames.TryParse<DeliveryChannel>(input.Channel, out var channel) ||
            !TextRules.HasLength(address, 1, Notification.MaxAddressLength))
            return Error.Validation("Выберите 1–50 записей, канал и тестовый адрес");

        var selected = await notices.GetManyForUpdateAsync(ids, ct);
        if (selected.Count != ids.Length)
            return Error.NotFound("Одно или несколько оповещений не найдены");

        var delivered = 0;
        foreach (var notice in selected)
        {
            var sent = await DeliverAsync(notice, channel, address, AuditActions.BulkDemoNotification, access, ct);
            if (sent.IsFailure) return sent.Error!;
            if (sent.Value.Status == NotificationStatus.Delivered) delivered++;
        }
        await unitOfWork.SaveChangesAsync(ct); // все записи пакета фиксируются вместе
        return new BulkNotificationResult(Simulated: true, selected.Count, delivered, selected.Count - delivered);
    }

    /// <summary>Ставит уведомление, доставляет его через шлюз и записывает попытки и аудит.</summary>
    private async Task<Result<Notification>> DeliverAsync(Notice notice, DeliveryChannel channel, string? address,
        string auditAction, UserAccess access, CancellationToken ct)
    {
        var queued = notice.QueueNotification(channel, address, clock.UtcNow);
        if (queued.IsFailure) return queued.Error!;
        var notification = queued.Value;

        var message = new OutgoingMessage(channel, notification.RecipientAddress, $"Оповещение № {notice.Number}", notice.Reason);
        notification.Apply(await gateway.DeliverAsync(message, ct));
        notice.Audit(auditAction, access.Username, clock.UtcNow, $"Канал: {channel} (имитация)");
        return notification;
    }
}
