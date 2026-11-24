using Adimarip.Domain.Common;
using Adimarip.Domain.Notices;
using Adimarip.Tests.Fakes;
using Xunit;

namespace Adimarip.Tests.Domain;

public sealed class NoticeTests
{
    [Fact]
    public void Register_creates_initial_history_and_audit()
    {
        var notice = Notice.Register(TestData.Draft(), "operator", TestData.Now).Value;

        Assert.Equal(NoticeStatus.Created, notice.CurrentStatus);
        Assert.Equal(NoticeStatus.Created, Assert.Single(notice.StatusHistory).Status);
        Assert.Equal(AuditActions.NoticeCreated, Assert.Single(notice.AuditEvents).Action);
        Assert.Equal("operator", notice.AuthorUserId);
    }

    [Fact]
    public void Register_rejects_creation_date_older_than_three_days()
    {
        var result = Notice.Register(TestData.Draft(created: TestData.Today.AddDays(-4)), "operator", TestData.Now);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
    }

    [Fact]
    public void Register_accepts_creation_date_exactly_three_days_ago() =>
        Assert.True(Notice.Register(TestData.Draft(created: TestData.Today.AddDays(-3)), "op", TestData.Now).IsSuccess);

    [Theory]
    [InlineData("AB", "Причина")]
    [InlineData("AD-1", "   ")]
    public void Register_validates_number_and_reason(string number, string reason) =>
        Assert.True(Notice.Register(TestData.Draft(number: number, reason: reason), "op", TestData.Now).IsFailure);

    [Fact]
    public void Register_rejects_deadline_before_creation() =>
        Assert.True(Notice.Register(TestData.Draft(deadline: TestData.Today.AddDays(-1)), "op", TestData.Now).IsFailure);

    [Fact]
    public void ChangeStatus_records_history_and_audit()
    {
        var notice = TestData.Notice();

        var result = notice.ChangeStatus(NoticeStatus.Sent, "manager", "  отправлено  ", TestData.Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(NoticeStatus.Sent, notice.CurrentStatus);
        Assert.Equal("отправлено", notice.StatusHistory.Last().Comment);
        Assert.Equal(AuditActions.StatusChanged, notice.AuditEvents.Last().Action);
    }

    [Fact]
    public void ChangeStatus_to_same_status_is_conflict()
    {
        var result = TestData.Notice().ChangeStatus(NoticeStatus.Created, "manager", null, TestData.Now);
        Assert.Equal(ErrorKind.Conflict, result.Error!.Kind);
    }

    [Theory]
    [InlineData(NoticeStatus.Created)]
    [InlineData(NoticeStatus.Sent)]
    [InlineData(NoticeStatus.Delivered)]
    public void Acknowledge_brings_notice_to_read_through_allowed_steps(NoticeStatus start)
    {
        var notice = TestData.Notice(start);

        var result = notice.Acknowledge("observer", TestData.Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(NoticeStatus.Read, notice.CurrentStatus);
        Assert.Equal(AuditActions.Acknowledged, notice.AuditEvents.Last().Action);
    }

    [Theory]
    [InlineData(NoticeStatus.Read)]
    [InlineData(NoticeStatus.Closed)]
    [InlineData(NoticeStatus.Cancelled)]
    public void Acknowledge_after_read_or_closing_is_conflict(NoticeStatus start) =>
        Assert.Equal(ErrorKind.Conflict, TestData.Notice(start).Acknowledge("observer", TestData.Now).Error!.Kind);

    [Fact]
    public void Delivery_outcome_with_one_delivered_channel_marks_delivered()
    {
        var notice = TestData.Notice();
        var email = notice.QueueNotification(DeliveryChannel.Email, "a@b.c", TestData.Now).Value;
        var sms = notice.QueueNotification(DeliveryChannel.Sms, "+70000000000", TestData.Now).Value;
        email.RecordAttempt(false, TestData.Now, "нет ящика");
        email.Complete(false);
        sms.RecordAttempt(true, TestData.Now);
        sms.Complete(true);

        Assert.True(notice.ApplyDeliveryOutcome(TestData.Now).IsSuccess);
        Assert.Equal(NoticeStatus.Delivered, notice.CurrentStatus);
    }

    [Fact]
    public void Delivery_outcome_with_all_channels_failed_marks_failed()
    {
        var notice = TestData.Notice();
        var email = notice.QueueNotification(DeliveryChannel.Email, "a@b.c", TestData.Now).Value;
        email.RecordAttempt(false, TestData.Now, "отказ");
        email.Complete(false);

        notice.ApplyDeliveryOutcome(TestData.Now);

        Assert.Equal(NoticeStatus.Failed, notice.CurrentStatus);
        Assert.Equal("отказ", email.LastError);
    }

    [Fact]
    public void Delivery_outcome_waits_for_pending_notifications()
    {
        var notice = TestData.Notice();
        notice.QueueNotification(DeliveryChannel.Portal, "portal:1", TestData.Now);

        Assert.True(notice.ApplyDeliveryOutcome(TestData.Now).IsFailure);
        Assert.Equal(NoticeStatus.Created, notice.CurrentStatus);
    }

    [Fact]
    public void Delivery_outcome_does_not_overwrite_acknowledgement()
    {
        var notice = TestData.Notice();
        var portal = notice.QueueNotification(DeliveryChannel.Portal, "portal:1", TestData.Now).Value;
        notice.Acknowledge("observer", TestData.Now);
        portal.RecordAttempt(true, TestData.Now);
        portal.Complete(true);

        Assert.True(notice.ApplyDeliveryOutcome(TestData.Now).IsSuccess);
        Assert.Equal(NoticeStatus.Read, notice.CurrentStatus);
    }

    [Theory]
    [InlineData("http://example.org/doc")]
    [InlineData("javascript:alert(1)")]
    public void AttachDocument_requires_https(string uri) =>
        Assert.True(TestData.Notice().AttachDocument("Док", "application/pdf", uri, "op", TestData.Now).IsFailure);

    [Fact]
    public void FileAppeal_requires_text()
    {
        Assert.True(TestData.Notice().FileAppeal("Clarification", "", "op", TestData.Now).IsFailure);
        Assert.Equal(Appeal.SubmittedStatus, TestData.Notice().FileAppeal("Clarification", "Текст", "op", TestData.Now).Value.Status);
    }
}
