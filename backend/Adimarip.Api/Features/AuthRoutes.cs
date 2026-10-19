using Adimarip.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Adimarip.Api.Features;

public sealed record LoginRequest(string? Username, string? Password);
public sealed record CreateAccountRequest(string? Username, string? DisplayName, string? Role, int? RecipientId, string? Password);

public static class AuthRoutes
{
    private static readonly HashSet<string> Roles = new(StringComparer.Ordinal) { "Operator", "Manager", "Observer", "AutomationEngineer" };

    public static IEndpointRouteBuilder MapRegistryAuth(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (LoginRequest request, UserStore store, AdimaripDbContext db, IConfiguration config, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrEmpty(request.Password))
                return Results.BadRequest(new { error = "Укажите логин и пароль" });
            var user = await store.FindAsync(request.Username.Trim(), ct);
            if (user is null || !UserStore.Verify(user, request.Password)) return Results.Unauthorized();
            string? recipientName = user.RecipientId is int rid
                ? await db.Recipients.AsNoTracking().Where(r => r.Id == rid).Select(r => r.FullName).FirstOrDefaultAsync(ct)
                : null;
            return Results.Ok(SecuritySetup.CreateToken(user, config, recipientName));
        }).AllowAnonymous().RequireRateLimiting("login");

        // Recipients list for the engineer: pick the recipient an Observer account will be bound to.
        app.MapGet("/api/admin/recipients", async (AdimaripDbContext db, CancellationToken ct) =>
        {
            var archivedIds = await db.Database.SqlQueryRaw<int>(
                "SELECT \"RecipientId\" AS \"Value\" FROM \"RecipientArchives\"").ToArrayAsync(ct);
            var list = await db.Recipients.AsNoTracking().Where(r => !archivedIds.Contains(r.Id))
                .OrderBy(r => r.FullName).Take(500)
                .Select(r => new { r.Id, r.ExternalIdentifier, r.FullName }).ToListAsync(ct);
            return Results.Ok(list);
        }).RequireAuthorization(SecuritySetup.Engineer);

        app.MapPost("/api/admin/users", async (CreateAccountRequest request, UserStore store, AdimaripDbContext db, CancellationToken ct) =>
        {
            var username = request.Username?.Trim() ?? "";
            var displayName = request.DisplayName?.Trim() ?? "";
            if (!System.Text.RegularExpressions.Regex.IsMatch(username, "^[a-zA-Z0-9_.-]{3,40}$") ||
                displayName.Length is < 2 or > 200 || !Roles.Contains(request.Role ?? "") ||
                request.Password is null || request.Password.Length < 10)
                return Results.BadRequest(new { error = "Проверьте логин, ФИО, роль и пароль (не менее 10 символов)" });
            if (request.Role == "Observer" && request.RecipientId is null)
                return Results.BadRequest(new { error = "Служащий должен быть привязан к адресату" });
            if (request.RecipientId is int id && !await db.Recipients.AnyAsync(r => r.Id == id, ct))
                return Results.BadRequest(new { error = "Неизвестный адресат" });
            try
            {
                bool created = await store.CreateAsync(username, displayName, request.Role!, request.RecipientId, request.Password, ct);
                return created ? Results.Created($"/api/admin/users/{Uri.EscapeDataString(username)}", new { username, role = request.Role }) :
                    Results.Conflict(new { error = "Такой логин уже существует" });
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
            {
                return Results.BadRequest(new { error = "Неизвестный адресат" });
            }
        }).RequireAuthorization(SecuritySetup.Engineer);
        return app;
    }
}
