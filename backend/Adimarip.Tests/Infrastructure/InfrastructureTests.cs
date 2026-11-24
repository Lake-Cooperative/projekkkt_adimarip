using Adimarip.Application.Delivery;
using Adimarip.Domain.Broadcasts;
using Adimarip.Domain.Notices;
using Adimarip.Infrastructure.Broadcasts;
using Adimarip.Infrastructure.Delivery;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Adimarip.Tests.Infrastructure;

public sealed class InMemoryBroadcastQueueTests
{
    [Fact]
    public async Task Critical_broadcasts_are_dequeued_first_then_fifo()
    {
        var queue = new InMemoryBroadcastQueue();
        queue.Enqueue(1, BroadcastSeverity.Info);
        queue.Enqueue(2, BroadcastSeverity.Info);
        queue.Enqueue(3, BroadcastSeverity.Critical);
        queue.Enqueue(4, BroadcastSeverity.Warning);

        var order = new List<int>();
        for (var i = 0; i < 4; i++) order.Add(await queue.DequeueAsync(CancellationToken.None));

        Assert.Equal(new[] { 3, 4, 1, 2 }, order.ToArray());
    }

    [Fact]
    public async Task Same_broadcast_is_queued_once()
    {
        var queue = new InMemoryBroadcastQueue();
        queue.Enqueue(7, BroadcastSeverity.Info);
        queue.Enqueue(7, BroadcastSeverity.Info);

        Assert.Equal(7, await queue.DequeueAsync(CancellationToken.None));
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await queue.DequeueAsync(timeout.Token));
    }
}

public sealed class SimulatedSenderTests
{
    private static readonly SimulationOptions Options = new() { LatencyMs = 0 };

    [Theory]
    [InlineData("user@example.invalid", true)]
    [InlineData("not-an-email", false)]
    [InlineData("fail-box@example.invalid", false)]
    public async Task Email_sender_validates_address(string address, bool delivered)
    {
        var sender = new SimulatedEmailSender(Options, NullLogger<SimulatedEmailSender>.Instance);
        var outcome = await sender.SendAsync(new OutgoingMessage(DeliveryChannel.Email, address, "s", "b"), CancellationToken.None);
        Assert.Equal(delivered, outcome.Delivered);
        Assert.False(outcome.Transient);
    }

    [Theory]
    [InlineData("+70000000000", true)]
    [InlineData("demo@example.invalid", false)]
    public async Task Sms_sender_requires_phone_number(string address, bool delivered)
    {
        var sender = new SimulatedSmsSender(Options, NullLogger<SimulatedSmsSender>.Instance);
        var outcome = await sender.SendAsync(new OutgoingMessage(DeliveryChannel.Sms, address, "s", "b"), CancellationToken.None);
        Assert.Equal(delivered, outcome.Delivered);
    }

    [Fact]
    public async Task Transient_failures_can_be_simulated()
    {
        var sender = new SimulatedPortalSender(new SimulationOptions { LatencyMs = 0, TransientFailureRate = 1 },
                NullLogger<SimulatedPortalSender>.Instance);
        var outcome = await sender.SendAsync(new OutgoingMessage(DeliveryChannel.Portal, "portal:1", "s", "b"), CancellationToken.None);
        Assert.True(outcome.Transient);
    }
}
