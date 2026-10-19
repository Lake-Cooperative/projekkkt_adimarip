using Adimarip.Data;
using Adimarip.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Adimarip.Api.Features;

public sealed record NotifyInput(string? Channel, string? RecipientAddress);
public sealed record AppealInput(string? Type, string? Text);
public sealed record DocumentInput(string? Name, string? MimeType, string? StorageUri);

public static class ActionRoutes
{
    private static readonly HashSet<string> Channels = new(StringComparer.Ordinal) { "Email", "Sms", "Portal", "Max" };

    public static IEndpointRouteBuilder MapRegistryActions(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/notices/{id:int}/notifications", async (int id, NotifyInput input,
            AdimaripDbContext db, HttpContext context, CancellationToken ct) =>
        {
            var address = input.RecipientAddress?.Trim() ?? "";
            if (!Channels.Contains(input.Channel ?? "") || address.Length is < 2 or > 320)
                return Results.BadRequest(new { error = "Недопустимый канал или адрес доставки" });
            if (!await db.Notices.AnyAsync(n => n.Id == id, ct)) return Results.NotFound(new { error = "Оповещение не найдено" });
            var notification = new Notification
            {
                NoticeId = id, Channel = input.Channel!, RecipientAddress = address, CreatedAt = DateTimeOffset.UtcNow
            };
            // Only simulation; no SMTP/SMS/portal APIs are called.
            notification.DeliveryAttempts.Add(new DeliveryAttempt { Status = "Delivered", AttemptedAt = DateTimeOffset.UtcNow });
            db.Notifications.Add(notification);
            db.AuditEvents.Add(new AuditEvent { NoticeId = id, Action = "DemoNotification", UserId = context.User.CurrentUserId(),
                OccurredAt = DateTimeOffset.UtcNow, Details = $"Channel: {input.Channel} (simulated)" });
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/notices/{id}", new { notification.Id, notification.Channel, simulated = true });
        }).RequireAuthorization(SecuritySetup.Writer);

        app.MapPost("/api/notices/{id:int}/appeals", async (int id, AppealInput input,
            AdimaripDbContext db, HttpContext context, CancellationToken ct) =>
        {
            var kind = input.Type?.Trim() ?? "";
            var text = input.Text?.Trim() ?? "";
            if (kind.Length is < 1 or > 100 || text.Length is < 1 or > 4000)
                return Results.BadRequest(new { error = "Укажите тип и текст обращения" });
            if (!await db.Notices.AnyAsync(n => n.Id == id, ct)) return Results.NotFound(new { error = "Оповещение не найдено" });
            var appeal = new Appeal { NoticeId = id, Type = kind, Text = text, Status = "Submitted", SubmittedAt = DateTimeOffset.UtcNow };
            db.Appeals.Add(appeal);
            db.AuditEvents.Add(new AuditEvent { NoticeId = id, Action = "AppealSubmitted", UserId = context.User.CurrentUserId(), OccurredAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/notices/{id}", new { appeal.Id, appeal.Status });
        }).RequireAuthorization(SecuritySetup.Writer);

        app.MapPost("/api/notices/{id:int}/documents", async (int id, DocumentInput input,
            AdimaripDbContext db, HttpContext context, CancellationToken ct) =>
        {
            var name = input.Name?.Trim() ?? "";
            var mime = input.MimeType?.Trim() ?? "";
            var uri = input.StorageUri?.Trim() ?? "";
            if (name.Length is < 1 or > 255 || mime.Length is < 3 or > 150 || uri.Length is < 6 or > 2048 ||
                !Uri.TryCreate(uri, UriKind.Absolute, out var parsed) || parsed.Scheme != Uri.UriSchemeHttps)
                return Results.BadRequest(new { error = "Проверьте имя, MIME-тип и HTTPS-ссылку на документ" });
            var notice = await db.Notices.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id, ct);
            if (notice is null || (context.User.IsInRole("Observer") && notice.RecipientId != context.User.CurrentRecipientId()))
                return Results.NotFound(new { error = "Оповещение не найдено" });
            var document = new Document { NoticeId = id, Name = name, MimeType = mime, StorageUri = uri, AddedAt = DateTimeOffset.UtcNow };
            db.Documents.Add(document);
            db.AuditEvents.Add(new AuditEvent { NoticeId = id, Action = "DocumentLinked", UserId = context.User.CurrentUserId(), OccurredAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/notices/{id}", new { document.Id, document.Name });
        }).RequireAuthorization(SecuritySetup.Reader);
        return app;
    }
}
