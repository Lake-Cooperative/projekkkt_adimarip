using Adimarip.Data;
using Adimarip.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Adimarip.Api.Features;

public sealed record LoginRequest(string? Username, string? Password);
public sealed record CreateAccountRequest(string? Username, string? DisplayName, string? Role, int? RecipientId, string? Password,
    DateOnly? DateOfBirth = null, string? Email = null, string? Phone = null, string? City = null, string? ExternalIdentifier = null);

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
            if (user is null || !UserStore.Verify(user, request.Password))
                return Results.Json(new { error = "Неверный логин или пароль" }, statusCode: StatusCodes.Status401Unauthorized);
            string? recipientName = user.RecipientId is int rid
                ? await db.Recipients.AsNoTracking().Where(r => r.Id == rid).Select(r => r.FullName).FirstOrDefaultAsync(ct)
                : null;
            return Results.Ok(SecuritySetup.CreateToken(user, config, recipientName));
        }).AllowAnonymous().RequireRateLimiting("login");

        // Observer (служащий) and recipient (адресат) are the same person: creating an Observer account
        // also creates the recipient card, and the recipient's ID becomes the observer's ID.
        app.MapPost("/api/admin/users", async (CreateAccountRequest request, UserStore store, AdimaripDbContext db, CancellationToken ct) =>
        {
            var username = request.Username?.Trim() ?? "";
            var displayName = request.DisplayName?.Trim() ?? "";
            if (!System.Text.RegularExpressions.Regex.IsMatch(username, "^[a-zA-Z0-9_.-]{3,40}$") ||
                displayName.Length is < 2 or > 200 || !Roles.Contains(request.Role ?? "") ||
                request.Password is null || request.Password.Length < 10)
                return Results.BadRequest(new { error = "Проверьте логин, ФИО, роль и пароль (не менее 10 символов)" });
            if (await store.FindAsync(username, ct) is not null)
                return Results.Conflict(new { error = "Такой логин уже существует" });

            if (request.Role != "Observer")
            {
                bool ok = await store.CreateAsync(username, displayName, request.Role!, null, request.Password, ct);
                return ok ? Results.Created($"/api/admin/users/{Uri.EscapeDataString(username)}", new { username, role = request.Role }) :
                    Results.Conflict(new { error = "Такой логин уже существует" });
            }

            if (!request.DateOfBirth.HasValue || request.DateOfBirth.Value > DateOnly.FromDateTime(DateTime.UtcNow) ||
                request.Email?.Length > 320 || request.Phone?.Length > 50 || displayName.Length > 250)
                return Results.BadRequest(new { error = "Укажите корректную дату рождения и контакты служащего" });
            var identifier = string.IsNullOrWhiteSpace(request.ExternalIdentifier)
                ? $"EMP-{username.ToUpperInvariant()}" : request.ExternalIdentifier.Trim();
            if (identifier.Length is < 2 or > 100)
                return Results.BadRequest(new { error = "Идентификатор адресата: от 2 до 100 символов" });
            if (await db.Recipients.AnyAsync(r => r.ExternalIdentifier == identifier, ct))
                return Results.Conflict(new { error = "Идентификатор адресата уже существует" });

            await using var tx = await db.Database.BeginTransactionAsync(ct);
            try
            {
                var recipient = new Recipient
                {
                    ExternalIdentifier = identifier, FullName = displayName, DateOfBirth = request.DateOfBirth.Value,
                    Email = request.Email?.Trim(), Phone = request.Phone?.Trim(),
                    Address = new Address { City = request.City?.Trim() }
                };
                db.Recipients.Add(recipient);
                await db.SaveChangesAsync(ct);
                bool created = await store.CreateAsync(username, displayName, "Observer", recipient.Id, request.Password, ct);
                if (!created)
                {
                    await tx.RollbackAsync(ct);
                    return Results.Conflict(new { error = "Такой логин уже существует" });
                }
                await tx.CommitAsync(ct);
                return Results.Created($"/api/admin/users/{Uri.EscapeDataString(username)}",
                    new { username, role = "Observer", recipientId = recipient.Id });
            }
            catch (DbUpdateException)
            {
                return Results.Conflict(new { error = "Не удалось создать служащего: идентификатор или логин уже заняты" });
            }
        }).RequireAuthorization(SecuritySetup.Engineer);
        return app;
    }
}
