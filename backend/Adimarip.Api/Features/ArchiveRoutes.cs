using Adimarip.Data;
using Microsoft.EntityFrameworkCore;

namespace Adimarip.Api.Features;

public sealed record ArchiveRecipientInput(string? Reason);

public static class ArchiveRoutes
{
    public static IEndpointRouteBuilder MapRegistryArchives(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/recipients/{id:int}/archive", async (int id, ArchiveRecipientInput input,
            AdimaripDbContext db, HttpContext context, CancellationToken ct) =>
        {
            var reason = input.Reason?.Trim() ?? "";
            if (reason.Length is < 3 or > 1000)
                return Results.BadRequest(new { error = "Укажите причину списания (3–1000 символов)" });
            if (!await db.Recipients.AnyAsync(r => r.Id == id, ct))
                return Results.NotFound(new { error = "Адресат не найден" });
            var author = context.User.CurrentUserId();
            var date = DateTimeOffset.UtcNow;
            var inserted = await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "RecipientArchives" ("RecipientId", "ArchivedAt", "ArchivedByUserId", "Reason")
                VALUES ({id}, {date}, {author}, {reason})
                ON CONFLICT ("RecipientId") DO NOTHING
                """, ct);
            if (inserted == 0) return Results.Conflict(new { error = "Адресат уже списан" });
            return Results.Ok(new { recipientId = id, archivedAt = date, reason });
        }).RequireAuthorization(SecuritySetup.Writer);
        return app;
    }
}
