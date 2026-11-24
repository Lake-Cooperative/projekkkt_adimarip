using Adimarip.Application.Broadcasts;
using Adimarip.Application.Delivery;
using Adimarip.Domain.Access;
using Adimarip.Domain.Broadcasts;
using Adimarip.Domain.Common;
using Adimarip.Domain.Notices;
using Adimarip.Tests.Fakes;
using Xunit;

namespace Adimarip.Tests.Application;

public sealed class BroadcastTests
{
    private readonly InMemoryStore _store = new();
    private readonly RecordingQueue _queue = new();
    private readonly FixedClock _clock = new(TestData.Now);
    private readonly DeliveryOptions _options = new()
    {
        SimulationEnabled = true,
        MaxAttempts = 2,
        RetryBaseDelayMs = 0,
        BatchSize = 2,
        MaxParallelism = 4
    };

    public BroadcastTests()
    {
        _store.Recipients.Add(TestData.Recipient(1));
        _store.Recipients.Add(TestData.Recipient(2, email: "fail-box@example.invalid", phone: null));
        _store.Recipients.Add(TestData.Recipient(3, email: null, phone: null));
        var archived = TestData.Recipient(4);
        archived.Archive("Уволен", "manager", TestData.Now);
        _store.Recipients.Add(archived);
    }

    private static readonly BroadcastInput Input = new("Учебная тревога", "Соберитесь у выхода №2", "Warning",
        ["Email"], 1, new AudienceInput("All", null, null));

    private BroadcastService Service(bool configured = true)
    {
        var gateway = Gateway(configured);
        return new BroadcastService(_store, _store, _store, _store, gateway, _queue, _store, _clock);
    }

    private INotificationGateway Gateway(bool configured = true)
    {
        var senders = DeliveryChannels.All.Select(channel => (IChannelSender)new ScriptedSender(channel)
        {
            ByMessage = m => m.Address.Contains("fail") ? SendOutcome.Permanent("Отказ") : SendOutcome.Success
        });
        return new NotificationGateway(senders, new DeliveryOptions { SimulationEnabled = configured, MaxAttempts = 2 }, _clock,
            (_, _) => Task.CompletedTask);
    }

    [Fact]
    public async Task Create_registers_notice_per_active_recipient_and_queues_dispatch()
    {
        var result = await Service().CreateAsync(TestData.Access(SystemRoles.Manager, username: "manager"), Input, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.TargetCount); // списанный адресат не оповещается
        var notices = _store.Notices.Where(n => n.BroadcastId == result.Value.Id).ToList();
        Assert.Equal(3, notices.Count);
        Assert.All(notices, n => Assert.Equal(AuditActions.BroadcastNoticeCreated, n.AuditEvents.Single().Action));
        Assert.All(notices, n => Assert.Contains("Внимание:", n.Reason));
        // Работник без контактов получает оповещение на портал.
        Assert.Equal(DeliveryChannel.Portal, notices.Single(n => n.RecipientId == 3).Notifications.Single().Channel);
        Assert.Equal((result.Value.Id, BroadcastSeverity.Warning), Assert.Single(_queue.Items));
    }

    [Fact]
    public async Task Create_is_refused_without_delivery_provider()
    {
        var result = await Service(configured: false).CreateAsync(TestData.Access(SystemRoles.Manager), Input, CancellationToken.None);
        Assert.Equal(ErrorKind.NotConfigured, result.Error!.Kind);
        Assert.Empty(_queue.Items);
    }

    [Fact]
    public async Task Create_validates_office_and_audience()
    {
        var service = Service();
        var manager = TestData.Access(SystemRoles.Manager);
        Assert.Equal(ErrorKind.NotFound, (await service.CreateAsync(manager, Input with { OfficeId = 99 },
                CancellationToken.None)).Error!.Kind);
        Assert.Equal(ErrorKind.Validation, (await service.CreateAsync(manager,
            Input with { Audience = new AudienceInput("Offices", [], null) }, CancellationToken.None)).Error!.Kind);
        Assert.Equal(ErrorKind.Validation, (await service.CreateAsync(manager,
            Input with { Audience = new AudienceInput("Recipients", null, [4]) }, CancellationToken.None)).Error!.Kind);
    }

    [Fact]
    public async Task Dispatcher_delivers_in_batches_and_completes_with_report()
    {
        var created = (await Service().CreateAsync(TestData.Access(SystemRoles.Manager), Input, CancellationToken.None)).Value;
        var dispatcher = new BroadcastDispatcher(_store, _store, Gateway(), _store, _options, _clock);

        await dispatcher.DispatchAsync(created.Id, CancellationToken.None);

        var notices = _store.Notices.Where(n => n.BroadcastId == created.Id).ToDictionary(n => n.RecipientId);
        Assert.Equal(NoticeStatus.Delivered, notices[1].CurrentStatus);
        Assert.Equal(NoticeStatus.Failed, notices[2].CurrentStatus);      // «сбойный» адрес
        Assert.Equal(NoticeStatus.Delivered, notices[3].CurrentStatus);   // резервный канал «Портал»
        Assert.All(notices.Values, n => Assert.DoesNotContain(n.Notifications, x => x.IsPending));

        var broadcast = _store.Broadcasts.Single();
        Assert.Equal(BroadcastStatus.CompletedWithErrors, broadcast.Status);
        Assert.Equal(2, broadcast.DeliveredCount);
        Assert.Equal(1, broadcast.FailedCount);
    }

    [Fact]
    public async Task Dispatcher_keeps_acknowledgement_made_during_delivery()
    {
        var created = (await Service().CreateAsync(TestData.Access(SystemRoles.Manager), Input, CancellationToken.None)).Value;
        var early = _store.Notices.First(n => n.BroadcastId == created.Id && n.RecipientId == 1);
        early.Acknowledge("observer", TestData.Now);

        await new BroadcastDispatcher(_store, _store, Gateway(), _store, _options, _clock).DispatchAsync(created.Id,
                CancellationToken.None);

        Assert.Equal(NoticeStatus.Read, early.CurrentStatus);
        Assert.Equal(NotificationStatus.Delivered, early.Notifications.Single().Status);
    }

    [Fact]
    public async Task Finished_broadcast_is_not_dispatched_twice()
    {
        var created = (await Service().CreateAsync(TestData.Access(SystemRoles.Manager), Input, CancellationToken.None)).Value;
        var dispatcher = new BroadcastDispatcher(_store, _store, Gateway(), _store, _options, _clock);
        await dispatcher.DispatchAsync(created.Id, CancellationToken.None);
        var saves = _store.SaveCount;

        await dispatcher.DispatchAsync(created.Id, CancellationToken.None);

        Assert.Equal(saves, _store.SaveCount);
    }
}
