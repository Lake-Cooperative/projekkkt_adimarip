namespace Adimarip.Application.Broadcasts;

public sealed record AudienceInput(string? Kind, int[]? OfficeIds, int[]? RecipientIds);

public sealed record BroadcastInput(
    string? Title,
    string? Message,
    string? Severity,
    string[]? Channels,
    int OfficeId,
    AudienceInput? Audience);

public sealed record AudiencePreview(int Recipients, int MaxAllowed);

public sealed record BroadcastCreated(int Id, string Code, string Status, int TargetCount, int PlannedNotifications);

public sealed record BroadcastSummary(
    int Id, string Code, string Title, string Severity, string Status, IReadOnlyList<string> Channels,
    string AudienceDescription, string CreatedByUserId, DateTimeOffset CreatedAt, DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt, int TargetCount, int DeliveredCount, int FailedCount);

public sealed record ChannelProgress(string Channel, int Delivered, int Failed, int Pending);

public sealed record BroadcastProgress(int Total, int Delivered, int Failed, int Pending, int Acknowledged,
    IReadOnlyList<ChannelProgress> Channels);

public sealed record BroadcastFailure(int NoticeId, string RecipientName, string Channel, string Address, string? Error);

public sealed record BroadcastDetails(BroadcastSummary Broadcast, BroadcastProgress Progress, IReadOnlyList<BroadcastFailure> Failures);
