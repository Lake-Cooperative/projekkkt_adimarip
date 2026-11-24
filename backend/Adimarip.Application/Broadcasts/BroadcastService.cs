using System.Security.Cryptography;
using Adimarip.Application.Abstractions;
using Adimarip.Application.Access;
using Adimarip.Application.Delivery;
using Adimarip.Domain.Broadcasts;
using Adimarip.Domain.Common;
using Adimarip.Domain.Notices;

namespace Adimarip.Application.Broadcasts;

/// <summary>
/// Массовое оповещение работников. Запрос только регистрирует рассылку и ставит её
/// в очередь (ответ 202 приходит сразу), а доставку параллельно выполняет
/// <see cref="BroadcastDispatcher"/> в фоне — так оповещение тысячи человек не
/// упирается в таймаут HTTP-запроса.
/// </summary>
public sealed class BroadcastService(
    IBroadcastRepository broadcasts,
    INoticeRepository notices,
    IRecipientRepository recipients,
    IOfficeRepository offices,
    INotificationGateway gateway,
    IBroadcastQueue queue,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public const int MaxAudience = 2000;
    private const int MaxFailuresInReport = 100;
    private const int RecentListSize = 50;

    public async Task<Result<AudiencePreview>> PreviewAsync(AudienceInput? input, CancellationToken ct)
    {
        var audience = BroadcastAudience.Create(input?.Kind, input?.OfficeIds, input?.RecipientIds);
        if (audience.IsFailure) return audience.Error!;
        return new AudiencePreview(await recipients.CountAudienceAsync(audience.Value, ct), MaxAudience);
    }

    public async Task<Result<BroadcastCreated>> CreateAsync(UserAccess access, BroadcastInput input, CancellationToken ct)
    {
        if (!gateway.IsConfigured)
            return Error.NotConfigured("Провайдер доставки не настроен: массовое оповещение работает только в демо-режиме");

        var audience = BroadcastAudience.Create(input.Audience?.Kind, input.Audience?.OfficeIds, input.Audience?.RecipientIds);
        if (audience.IsFailure) return audience.Error!;
        if (!await offices.ExistsAsync(input.OfficeId, ct))
            return Error.NotFound("Подразделение-отправитель не найдено");

        var targetCount = await recipients.CountAudienceAsync(audience.Value, ct);
        if (targetCount > MaxAudience)
            return Error.Validation($"Слишком большая аудитория: {targetCount} (не более {MaxAudience} за одну рассылку)");

        var now = clock.UtcNow;
        var draft = new BroadcastDraft(input.Title, input.Message, input.Severity, input.Channels, input.OfficeId);
        var created = Broadcast.Create(draft, audience.Value, targetCount, access.Username, now, NewCode(now));
        if (created.IsFailure) return created.Error!;

        var contacts = await recipients.GetAudienceAsync(audience.Value, ct);
        var registered = await RegisterAsync(created.Value, contacts, access.Username, now, ct);
        if (registered.IsSuccess)
            queue.Enqueue(created.Value.Id, created.Value.Severity); // только после фиксации транзакции
        return registered;
    }

    /// <summary>Рассылка и оповещения всех работников сохраняются одной транзакцией.</summary>
    private Task<Result<BroadcastCreated>> RegisterAsync(Broadcast broadcast, IReadOnlyList<RecipientContacts> contacts,
        string author, DateTimeOffset now, CancellationToken ct) =>
        unitOfWork.InTransactionAsync<BroadcastCreated>(async () =>
        {
            broadcasts.Add(broadcast);
            await unitOfWork.SaveChangesAsync(ct); // нужен Id рассылки для номеров оповещений
            var planned = 0;
            foreach (var person in contacts)
            {
                var notice = CreateNoticeFor(broadcast, person, author, now);
                if (notice.IsFailure) return notice.Error!;
                planned += notice.Value.Notifications.Count;
                notices.Add(notice.Value);
            }
            await unitOfWork.SaveChangesAsync(ct);
            return new BroadcastCreated(broadcast.Id, broadcast.Code, broadcast.Status.ToString(), contacts.Count, planned);
        }, TransactionMode.ReadCommitted, "Не удалось зарегистрировать рассылку: повторите попытку", ct);

    public async Task<IReadOnlyList<BroadcastSummary>> ListAsync(CancellationToken ct) =>
        (await broadcasts.ListRecentAsync(RecentListSize, ct)).Select(ToSummary).ToArray();

    public async Task<Result<BroadcastDetails>> GetAsync(int id, CancellationToken ct)
    {
        var broadcast = await broadcasts.GetAsync(id, ct);
        if (broadcast is null) return Error.NotFound("Рассылка не найдена");
        var progress = await notices.GetBroadcastProgressAsync(id, ct);
        var failures = await notices.GetBroadcastFailuresAsync(id, MaxFailuresInReport, ct);
        return new BroadcastDetails(ToSummary(broadcast), progress, failures);
    }

    private static Result<Notice> CreateNoticeFor(Broadcast broadcast, RecipientContacts person, string author, DateTimeOffset now)
    {
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var draft = new NoticeDraft(broadcast.NoticeNumberFor(person.RecipientId), person.RecipientId, broadcast.OfficeId,
            today, today.AddDays(broadcast.ProcessingDays), ReasonFor(broadcast), broadcast.Message);
        var notice = Notice.Register(draft, author, now, broadcast.Id, AuditActions.BroadcastNoticeCreated);
        if (notice.IsFailure) return notice;

        foreach (var delivery in DeliveryPlanner.Plan(person, broadcast.Channels, broadcast.Severity))
        {
            var queued = notice.Value.QueueNotification(delivery.Channel, delivery.Address, now);
            if (queued.IsFailure) return queued.Error!;
        }
        return notice;
    }

    internal static string ReasonFor(Broadcast broadcast) => broadcast.Severity switch
    {
        BroadcastSeverity.Critical => $"СРОЧНО: {broadcast.Title}",
        BroadcastSeverity.Warning => $"Внимание: {broadcast.Title}",
        _ => broadcast.Title
    };

    internal static string NewCode(DateTimeOffset now) =>
        $"MO-{now:yyMMdd}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(3))}";

    private static BroadcastSummary ToSummary(Broadcast b) => new(
        b.Id, b.Code, b.Title, b.Severity.ToString(), b.Status.ToString(), b.Channels.Select(c => c.ToString()).ToArray(),
        b.AudienceDescription, b.CreatedByUserId, b.CreatedAt, b.StartedAt, b.CompletedAt,
        b.TargetCount, b.DeliveredCount, b.FailedCount);
}
