using Adimarip.Domain.Broadcasts;
using Adimarip.Domain.Notices;
using Adimarip.Tests.Fakes;
using Xunit;

namespace Adimarip.Tests.Domain;

public sealed class BroadcastTests
{
    private static readonly BroadcastAudience Everyone = BroadcastAudience.Create("All", null, null).Value;

    [Fact]
    public void Planner_uses_requested_channels_with_known_contacts()
    {
        var plan = DeliveryPlanner.Plan(new RecipientContacts(1, "Иванов", "i@example.invalid", null),
            [DeliveryChannel.Email, DeliveryChannel.Sms], BroadcastSeverity.Info);

        var single = Assert.Single(plan);
        Assert.Equal(DeliveryChannel.Email, single.Channel);
        Assert.Equal("i@example.invalid", single.Address);
    }

    [Fact]
    public void Planner_falls_back_to_portal_when_no_contact_matches()
    {
        var plan = DeliveryPlanner.Plan(new RecipientContacts(42, "Без контактов", null, null), [DeliveryChannel.Sms],
                BroadcastSeverity.Warning);

        Assert.Equal(new PlannedDelivery(DeliveryChannel.Portal, "portal:42"), Assert.Single(plan));
    }

    [Fact]
    public void Critical_broadcast_uses_every_available_channel()
    {
        var plan = DeliveryPlanner.Plan(new RecipientContacts(1, "Иванов", "i@example.invalid", "+70000000001"),
            [DeliveryChannel.Email], BroadcastSeverity.Critical);

        Assert.Equal(DeliveryChannels.All.ToArray(), plan.Select(p => p.Channel).ToArray());
    }

    [Theory]
    [InlineData("Offices")]
    [InlineData("Recipients")]
    [InlineData("Everybody")]
    public void Audience_requires_valid_kind_and_targets(string kind) =>
        Assert.True(BroadcastAudience.Create(kind, null, null).IsFailure);

    [Fact]
    public void Audience_by_offices_is_deduplicated()
    {
        var audience = BroadcastAudience.Create("Offices", [2, 2, 3], null).Value;
        Assert.Equal(new[] { 2, 3 }, audience.OfficeIds.ToArray());
    }

    [Fact]
    public void Broadcast_requires_channels_and_known_severity()
    {
        Assert.True(Create(severity: "Panic").IsFailure);
        Assert.True(Create(channels: []).IsFailure);
        Assert.True(Create(channels: ["Pigeon"]).IsFailure);
        Assert.True(Create(targetCount: 0).IsFailure);
    }

    [Fact]
    public void Broadcast_lifecycle_reports_errors()
    {
        var broadcast = Create().Value;
        Assert.Equal(BroadcastStatus.Queued, broadcast.Status);

        broadcast.StartDispatch(TestData.Now);
        broadcast.Complete(delivered: 9, failed: 1, TestData.Now);

        Assert.Equal(BroadcastStatus.CompletedWithErrors, broadcast.Status);
        Assert.True(broadcast.IsFinished);
        Assert.Equal("MO-TEST-5", broadcast.NoticeNumberFor(5));
    }

    [Fact]
    public void Critical_broadcast_has_shorter_deadline() =>
        Assert.Equal(1, Create(severity: "Critical").Value.ProcessingDays);

    private static Adimarip.Domain.Common.Result<Broadcast> Create(string severity = "Warning", string[]? channels = null,
            int targetCount = 10) =>
        Broadcast.Create(new BroadcastDraft("Учебная тревога", "Покиньте здание", severity, channels ?? ["Email", "Sms"], 1),
            Everyone, targetCount, "manager", TestData.Now, "MO-TEST");
}
