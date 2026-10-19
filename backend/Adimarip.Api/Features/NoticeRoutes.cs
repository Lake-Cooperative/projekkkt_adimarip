using Adimarip.Data;
using Adimarip.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Adimarip.Api.Features;

public sealed record CreateNoticeInput(string? Number, int RecipientId, int OfficeId,
    DateOnly? CreatedAt, DateOnly? ProcessingDeadline, string? Reason, string? Comment);

public static class NoticeRoutes
{
    public static IEndpointRouteBuilder MapRegistryNotices(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/notices", async (string? status, int? officeId, string? search,
            int? page, int? pageSize, AdimaripDbContext db, HttpContext context, CancellationToken ct) =>
        {
            var currentPage = page ?? 1;
            var size = pageSize ?? 15;
            if (currentPage < 1 || size is < 1 or > 100 || (search?.Length ?? 0) > 100)
                return Results.BadRequest(new { error = "Некорректные параметры поиска или пагинации" });
            IQueryable<Notice> query = db.Notices.AsNoTracking();
            if (context.User.IsInRole("Observer"))
            {
                var own = context.User.CurrentRecipientId();
                query = query.Where(n => own.HasValue && n.RecipientId == own.Value);
            }
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(n => n.CurrentStatus == status);
            if (officeId.HasValue) query = query.Where(n => n.OfficeId == officeId.Value);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = $"%{search.Trim()}%";
                query = query.Where(n => EF.Functions.ILike(n.Number, pattern) ||
                    EF.Functions.ILike(n.Reason, pattern) || EF.Functions.ILike(n.Recipient.FullName, pattern));
            }
            var total = await query.CountAsync(ct);
            var items = await query.OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id)
                .Skip((currentPage - 1) * size).Take(size)
                .Select(n => new
                {
                    n.Id, n.Number, n.RecipientId, recipientName = n.Recipient.FullName,
                    n.OfficeId, officeName = n.Office.Name, n.CreatedAt, n.ProcessingDeadline,
                    n.Reason, n.Comment, n.CurrentStatus
                }).ToListAsync(ct);
            return Results.Ok(new { items, total, page = currentPage, pageSize = size, totalPages = (total + size - 1) / size });
        }).RequireAuthorization(SecuritySetup.Reader);

        app.MapGet("/api/notices/{id:int}", async (int id, AdimaripDbContext db, HttpContext context, CancellationToken ct) =>
        {
            var n = await db.Notices.AsNoTracking().AsSplitQuery()
                .Include(x => x.Recipient).Include(x => x.Office)
                .Include(x => x.StatusHistory).Include(x => x.AuditEvents)
                .Include(x => x.Notifications).ThenInclude(x => x.DeliveryAttempts)
                .Include(x => x.Appeals).Include(x => x.Documents)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (n is null || (context.User.IsInRole("Observer") && n.RecipientId != context.User.CurrentRecipientId()))
                return Results.NotFound(new { error = "Оповещение не найдено" });
            return Results.Ok(new
            {
                n.Id, n.Number, n.RecipientId, recipientName = n.Recipient.FullName,
                n.OfficeId, officeName = n.Office.Name, n.CreatedAt, n.ProcessingDeadline, n.Reason,
                n.Comment, n.CurrentStatus,
                history = context.User.IsInRole("Observer") ? Array.Empty<object>() :
                    n.StatusHistory.OrderByDescending(h => h.ChangedAt).Select(h => (object)new
                    { h.Id, h.Status, h.Comment, h.ChangedAt, h.ChangedByUserId }).ToArray(),
                audit = context.User.IsInRole("Manager") ?
                    n.AuditEvents.OrderByDescending(a => a.OccurredAt).Select(a => new
                    { a.Id, a.Action, a.UserId, a.OccurredAt, a.Details }).ToArray() : null,
                notifications = n.Notifications.OrderByDescending(x => x.CreatedAt).Select(x => new
                {
                    x.Id, x.Channel, x.RecipientAddress, x.CreatedAt,
                    attempts = x.DeliveryAttempts.OrderByDescending(a => a.AttemptedAt).Select(a => new { a.Status, a.AttemptedAt })
                }),
                appeals = n.Appeals.OrderByDescending(x => x.SubmittedAt).Select(x => new
                { x.Id, x.Type, x.Text, x.Status, x.SubmittedAt }),
                documents = n.Documents.OrderByDescending(x => x.AddedAt).Select(x => new
                { x.Id, x.Name, x.MimeType, x.StorageUri, x.AddedAt })
            });
        }).RequireAuthorization(SecuritySetup.Reader);

        app.MapPost("/api/notices", async (CreateNoticeInput input, AdimaripDbContext db, HttpContext context, CancellationToken ct) =>
        {
            var number = input.Number?.Trim() ?? "";
            var reason = input.Reason?.Trim() ?? "";
            var created = input.CreatedAt ?? DateOnly.FromDateTime(DateTime.UtcNow);
            if (number.Length is < 3 or > 100 || reason.Length is < 1 or > 1000 ||
                input.Comment?.Length > 4000 || !input.ProcessingDeadline.HasValue ||
                input.ProcessingDeadline.Value < created)
                return Results.BadRequest(new { error = "Проверьте номер, причину и срок обработки" });
            if (!await db.Recipients.AnyAsync(r => r.Id == input.RecipientId, ct) ||
                !await db.Offices.AnyAsync(o => o.Id == input.OfficeId, ct))
                return Results.NotFound(new { error = "Адресат или подразделение не найдены" });
            if (await db.Notices.AnyAsync(n => n.Number == number, ct))
                return Results.Conflict(new { error = "Номер оповещения занят" });
            var notice = new Notice
            {
                Number = number, RecipientId = input.RecipientId, OfficeId = input.OfficeId,
                AuthorUserId = context.User.CurrentUserId(), CreatedAt = created,
                ProcessingDeadline = input.ProcessingDeadline.Value, Reason = reason,
                Comment = input.Comment?.Trim(), CurrentStatus = "Created"
            };
            notice.StatusHistory.Add(new NoticeStatusHistory
                { Status = "Created", ChangedAt = DateTimeOffset.UtcNow, ChangedByUserId = context.User.CurrentUserId() });
            notice.AuditEvents.Add(new AuditEvent
                { Action = "NoticeCreated", UserId = context.User.CurrentUserId(), OccurredAt = DateTimeOffset.UtcNow });
            db.Notices.Add(notice);
            try
            {
                await db.SaveChangesAsync(ct); // EF saves the notice + history + audit in a single transaction.
                return Results.Created($"/api/notices/{notice.Id}", new { notice.Id, notice.Number, notice.CurrentStatus });
            }
            catch (DbUpdateException)
            {
                return Results.Conflict(new { error = "Номер уже существует или данные не прошли ограничения БД" });
            }
        }).RequireAuthorization(SecuritySetup.Writer);
        return app;
    }
}
