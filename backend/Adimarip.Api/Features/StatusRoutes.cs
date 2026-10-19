using Adimarip.Data;
using Adimarip.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;

namespace Adimarip.Api.Features;

public sealed record ChangeStatusInput(string? Status, string? Comment);

public static class StatusRoutes
{
    private static readonly IReadOnlyDictionary<string, string[]> Transitions = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["Created"] = ["Sent", "Cancelled"],
        ["Sent"] = ["Delivered", "Failed", "Cancelled"],
        ["Failed"] = ["Sent", "Cancelled"],
        ["Delivered"] = ["Read", "Closed"],
        ["Read"] = ["Closed"],
        ["Closed"] = [],
        ["Cancelled"] = []
    };

    public static bool CanTransition(string from, string to) =>
        Transitions.TryGetValue(from, out var next) && next.Contains(to, StringComparer.Ordinal);

    public static IEndpointRouteBuilder MapRegistryStatuses(this IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/notices/{id:int}/status", async (int id, ChangeStatusInput input,
            AdimaripDbContext db, HttpContext context, CancellationToken ct) =>
        {
            var status = input.Status?.Trim() ?? "";
            if (status.Length is < 2 or > 50 || input.Comment?.Length > 4000)
                return Results.BadRequest(new { error = "Не указан статус или слишком длинный комментарий" });
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            try
            {
                var notice = await db.Notices.FirstOrDefaultAsync(n => n.Id == id, ct);
                if (notice is null) return Results.NotFound(new { error = "Оповещение не найдено" });
                if (!CanTransition(notice.CurrentStatus, status))
                    return Results.Conflict(new { error = $"Переход {notice.CurrentStatus} → {status} не разрешён" });
                notice.CurrentStatus = status;
                db.NoticeStatusHistories.Add(new NoticeStatusHistory
                {
                    NoticeId = notice.Id, Status = status, Comment = input.Comment?.Trim(),
                    ChangedAt = DateTimeOffset.UtcNow, ChangedByUserId = context.User.CurrentUserId()
                });
                db.AuditEvents.Add(new AuditEvent
                {
                    NoticeId = notice.Id, Action = "StatusChanged", UserId = context.User.CurrentUserId(),
                    OccurredAt = DateTimeOffset.UtcNow, Details = $"Status: {status}"
                });
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return Results.Ok(new { notice.Id, notice.CurrentStatus });
            }
            catch (DbUpdateException)
            {
                return Results.Conflict(new { error = "Конфликт параллельного изменения записи" });
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.SerializationFailure)
            {
                return Results.Conflict(new { error = "Запись изменена параллельно. Обновите карточку" });
            }
        }).RequireAuthorization(SecuritySetup.Writer);
        return app;
    }
}
