using Adimarip.Domain.Common;
using Adimarip.Domain.Notices;
using Xunit;

namespace Adimarip.Tests.Domain;

public sealed class NoticeLifecycleTests
{
    [Theory]
    [InlineData(NoticeStatus.Created, NoticeStatus.Sent)]
    [InlineData(NoticeStatus.Sent, NoticeStatus.Delivered)]
    [InlineData(NoticeStatus.Sent, NoticeStatus.Failed)]
    [InlineData(NoticeStatus.Failed, NoticeStatus.Sent)]
    [InlineData(NoticeStatus.Delivered, NoticeStatus.Read)]
    [InlineData(NoticeStatus.Read, NoticeStatus.Closed)]
    public void Allows_documented_transitions(NoticeStatus from, NoticeStatus to) =>
        Assert.True(NoticeLifecycle.CanTransition(from, to));

    [Theory]
    [InlineData(NoticeStatus.Created, NoticeStatus.Created)]
    [InlineData(NoticeStatus.Created, NoticeStatus.Delivered)]
    [InlineData(NoticeStatus.Closed, NoticeStatus.Sent)]
    [InlineData(NoticeStatus.Cancelled, NoticeStatus.Created)]
    public void Rejects_repeats_skips_and_terminal_exits(NoticeStatus from, NoticeStatus to) =>
        Assert.False(NoticeLifecycle.CanTransition(from, to));

    [Fact]
    public void Terminal_statuses_have_no_transitions()
    {
        Assert.Empty(NoticeLifecycle.AllowedFrom(NoticeStatus.Closed));
        Assert.Empty(NoticeLifecycle.AllowedFrom(NoticeStatus.Cancelled));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("sent")]
    [InlineData(" Sent")]
    [InlineData("")]
    public void Status_names_are_parsed_strictly(string raw) =>
        Assert.False(EnumNames.TryParse<NoticeStatus>(raw, out _));

    [Fact]
    public void Exact_status_name_is_parsed() =>
        Assert.True(EnumNames.TryParse<NoticeStatus>("Sent", out var status) && status == NoticeStatus.Sent);
}
