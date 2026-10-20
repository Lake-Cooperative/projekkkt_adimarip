using Adimarip.Data;
using Adimarip.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Adimarip.Api.Features;

public sealed record RecipientInput(string? ExternalIdentifier, string? FullName, DateOnly? DateOfBirth,
    string? Email, string? Phone, AddressInput? Address);
public sealed record AddressInput(string? PostalCode, string? Region, string? City, string? Street, string? House, string? Apartment);

public static class DirectoryRoutes
{
    public static IEndpointRouteBuilder MapRegistryDirectories(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/offices", async (AdimaripDbContext db, UserStore accounts,
            HttpContext context, CancellationToken ct) =>
        {
            var query = db.Offices.AsNoTracking();
            if (context.User.IsInRole("Observer"))
            {
                var ownerId = await ObserverAccess.GetVerifiedRecipientIdAsync(context.User, accounts, ct);
                if (!ownerId.HasValue) return Results.Forbid();
                query = query.Where(o => db.Notices.Any(n => n.OfficeId == o.Id && n.RecipientId == ownerId.Value));
            }
            return Results.Ok(await query.OrderBy(o => o.Name)
                .Select(o => new { o.Id, o.Name }).ToListAsync(ct));
        }).RequireAuthorization(SecuritySetup.Reader);

        app.MapGet("/api/recipients", async (string? search, AdimaripDbContext db,
            UserStore accounts, HttpContext context, CancellationToken ct) =>
        {
            var archivedIds = await db.Database.SqlQueryRaw<int>(
                "SELECT \"RecipientId\" AS \"Value\" FROM \"RecipientArchives\"").ToArrayAsync(ct);
            var query = db.Recipients.AsNoTracking().Where(r => !archivedIds.Contains(r.Id));
            if (context.User.IsInRole("Observer"))
            {
                var ownerId = await ObserverAccess.GetVerifiedRecipientIdAsync(context.User, accounts, ct);
                if (!ownerId.HasValue) return Results.Forbid();
                query = query.Where(r => r.Id == ownerId.Value);
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                if (term.Length > 100) return Results.BadRequest(new { error = "Строка поиска слишком длинная" });
                query = query.Where(r => EF.Functions.ILike(r.FullName, $"%{term}%") || EF.Functions.ILike(r.ExternalIdentifier, $"%{term}%"));
            }
            var recipients = await query.OrderBy(r => r.FullName).Take(100)
                .Select(r => new { r.Id, r.ExternalIdentifier, r.FullName, r.DateOfBirth, r.Email, r.Phone })
                .ToListAsync(ct);
            return Results.Ok(recipients);
        }).RequireAuthorization(SecuritySetup.Reader);

        app.MapPost("/api/recipients", async (RecipientInput input, AdimaripDbContext db, CancellationToken ct) =>
        {
            var identifier = input.ExternalIdentifier?.Trim() ?? "";
            var fullName = input.FullName?.Trim() ?? "";
            if (identifier.Length is < 2 or > 100 || fullName.Length is < 2 or > 250 ||
                !input.DateOfBirth.HasValue || input.DateOfBirth.Value > DateOnly.FromDateTime(DateTime.UtcNow) ||
                input.Email?.Length > 320 || input.Phone?.Length > 50)
                return Results.BadRequest(new { error = "Проверьте идентификатор, ФИО, дату рождения и контакты" });
            if (await db.Recipients.AnyAsync(r => r.ExternalIdentifier == identifier, ct))
                return Results.Conflict(new { error = "Идентификатор уже существует" });
            var recipient = new Recipient
            {
                ExternalIdentifier = identifier, FullName = fullName, DateOfBirth = input.DateOfBirth.Value,
                Email = input.Email?.Trim(), Phone = input.Phone?.Trim(),
                Address = new Address
                {
                    PostalCode = input.Address?.PostalCode?.Trim(), Region = input.Address?.Region?.Trim(),
                    City = input.Address?.City?.Trim(), Street = input.Address?.Street?.Trim(),
                    House = input.Address?.House?.Trim(), Apartment = input.Address?.Apartment?.Trim()
                }
            };
            db.Recipients.Add(recipient);
            try
            {
                await db.SaveChangesAsync(ct);
                return Results.Created($"/api/recipients/{recipient.Id}", new { recipient.Id, recipient.FullName, recipient.ExternalIdentifier });
            }
            catch (DbUpdateException)
            {
                return Results.Conflict(new { error = "Не удалось сохранить: идентификатор уже существует или данные некорректны" });
            }
        }).RequireAuthorization(SecuritySetup.Engineer);
        return app;
    }
}
