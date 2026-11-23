using Adimarip.Application.Delivery;
using Adimarip.Domain.Notices;
using Adimarip.Tests.Fakes;
using Xunit;

namespace Adimarip.Tests.Application;

public sealed class NotificationGatewayTests
{
    private static readonly DeliveryOptions Options = new() { SimulationEnabled = true, MaxAttempts = 3, RetryBaseDelayMs = 1 };
    private static readonly OutgoingMessage Email = new(DeliveryChannel.Email, "a@example.invalid", "Тема", "Текст");

    private static NotificationGateway Gateway(IChannelSender sender, List<TimeSpan>? delays = null) =>
        new([sender], Options, new FixedClock(TestData.Now), (delay, _) => { delays?.Add(delay); return Task.CompletedTask; });

    [Fact]
    public async Task Transient_failures_are_retried_with_backoff()
    {
        var delays = new List<TimeSpan>();
        var sender = new ScriptedSender(DeliveryChannel.Email, SendOutcome.Temporary("503"), SendOutcome.Temporary("503"),
                SendOutcome.Success);

        var report = await Gateway(sender, delays).DeliverAsync(Email, CancellationToken.None);

        Assert.True(report.Delivered);
        Assert.Equal(3, report.Attempts.Count);
        Assert.Equal(new[] { TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(2) }, delays.ToArray());
    }

    [Fact]
    public async Task Urgent_messages_retry_without_growing_delay()
    {
        var delays = new List<TimeSpan>();
        var sender = new ScriptedSender(DeliveryChannel.Email, SendOutcome.Temporary("503"));

        await Gateway(sender, delays).DeliverAsync(Email with { Urgent = true }, CancellationToken.None);

        Assert.All(delays, d => Assert.Equal(TimeSpan.FromMilliseconds(1), d));
    }

    [Fact]
    public async Task Permanent_failure_is_not_retried()
    {
        var sender = new ScriptedSender(DeliveryChannel.Email, SendOutcome.Permanent("Некорректный e-mail"));

        var report = await Gateway(sender).DeliverAsync(Email, CancellationToken.None);

        Assert.False(report.Delivered);
        Assert.Equal(1, sender.Calls);
        Assert.Equal("Некорректный e-mail", report.LastError);
    }

    [Fact]
    public async Task Gives_up_after_max_attempts()
    {
        var sender = new ScriptedSender(DeliveryChannel.Email, SendOutcome.Temporary("timeout"));

        var report = await Gateway(sender).DeliverAsync(Email, CancellationToken.None);

        Assert.False(report.Delivered);
        Assert.Equal(Options.MaxAttempts, sender.Calls);
    }

    [Fact]
    public async Task Unknown_channel_fails_without_exception()
    {
        var report = await Gateway(new ScriptedSender(DeliveryChannel.Sms)).DeliverAsync(Email, CancellationToken.None);
        Assert.False(report.Delivered);
    }

    [Fact]
    public void Not_configured_outside_demo_mode() =>
        Assert.False(new NotificationGateway([new ScriptedSender(DeliveryChannel.Email)], new DeliveryOptions(),
                new FixedClock(TestData.Now)).IsConfigured);
}
