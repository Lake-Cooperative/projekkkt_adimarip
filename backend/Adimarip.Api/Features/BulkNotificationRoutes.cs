using Adimarip.Data;
using Adimarip.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Adimarip.Api.Features;

public sealed record BulkNotifyInput(int[]? NoticeIds, string? Channel, string? RecipientAddress);

public static class BulkNotificationRoutes
{
    public static IEndpointRouteBuilder MapBulkNotifications(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/notices/notifications/bulk", async (BulkNotifyInput input,
            AdimaripDbContext db, HttpContext context, CancellationToken ct) =>
        {
            var ids = input.NoticeIds?.Distinct().ToArray() ?? [];
            if (ids.Length is < 1 or > 50 || ids.Any(id => id < 1) ||
                input.Channel is not ("Email" or "Sms" or "Portal" or "Max") ||
                string.IsNullOrWhiteSpace(input.RecipientAddress) || input.RecipientAddress.Length > 320)
                return Results.BadRequest(new { error = "Выберите 1–50 записей, канал и тестовый адрес" });
            var existingIds = await db.Notices.Where(n => ids.Contains(n.Id)).Select(n => n.Id).ToArrayAsync(ct);
            if (existingIds.Length != ids.Length)
                return Results.NotFound(new { error = "Одно или несколько оповещений не найдены" });
            foreach (var id in existingIds)
            {
                var notification = new Notification
                {
                    NoticeId = id, Channel = input.Channel, RecipientAddress = input.RecipientAddress.Trim(),
                    CreatedAt = DateTimeOffset.UtcNow
                };
                notification.DeliveryAttempts.Add(new DeliveryAttempt { Status = "Delivered", AttemptedAt = DateTimeOffset.UtcNow });
                db.Notifications.Add(notification);
                db.AuditEvents.Add(new AuditEvent
                {
                    NoticeId = id, Action = "BulkDemoNotification", UserId = context.User.CurrentUserId(),
                    OccurredAt = DateTimeOffset.UtcNow, Details = $"Channel: {input.Channel}; simulated only"
                });
            }
            // All records are committed together. A real sender would need durable outbox + retries.
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { simulated = true, count = existingIds.Length });
        }).RequireAuthorization(SecuritySetup.Writer);
        return app;
    }
}
