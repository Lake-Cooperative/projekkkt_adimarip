using Adimarip.Application.Abstractions;
using Adimarip.Application.Access;
using Adimarip.Domain.Access;
using Microsoft.EntityFrameworkCore;

namespace Adimarip.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(AdimaripDbContext db) : IUserRepository
{
    public Task<AppUser?> FindAsync(string username, CancellationToken ct) =>
        db.Users.FirstOrDefaultAsync(u => u.Username == username, ct);

    public Task<AppUser?> FindForLoginAsync(string username, CancellationToken ct)
    {
        var lowered = username.ToLowerInvariant();
        return db.Users.AsNoTracking()
            .Where(u => u.Username.ToLower() == lowered)
            .OrderByDescending(u => u.Username == username)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<UserListItem>> ListAsync(CancellationToken ct) =>
        await db.Users.AsNoTracking()
            .OrderBy(u => u.Username)
            .Select(u => new UserListItem(u.Username, u.DisplayName, u.RoleName, u.RecipientId, u.CreatedAt))
            .ToListAsync(ct);

    public Task<int> CountWithRoleAsync(string roleName, bool withoutRecipientOnly, CancellationToken ct) =>
        db.Users.CountAsync(u => u.RoleName == roleName && (!withoutRecipientOnly || u.RecipientId == null), ct);

    public async Task<IReadOnlyDictionary<string, int>> CountByRoleAsync(CancellationToken ct) =>
        await db.Users.AsNoTracking()
            .GroupBy(u => u.RoleName)
            .Select(g => new { Role = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Role, x => x.Count, ct);

    public void Add(AppUser user) => db.Users.Add(user);
}

internal sealed class RoleRepository(AdimaripDbContext db) : IRoleRepository
{
    public async Task<IReadOnlyList<Role>> ListAsync(CancellationToken ct) =>
        await db.Roles.AsNoTracking().Include(r => r.Permissions).ToListAsync(ct);

    public Task<Role?> GetAsync(string name, CancellationToken ct) =>
        db.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Name == name, ct);

    public Task<bool> ExistsAsync(string name, CancellationToken ct) =>
        db.Roles.AnyAsync(r => r.Name.ToLower() == name.ToLower(), ct);

    public void Add(Role role) => db.Roles.Add(role);

    public void Remove(Role role) => db.Roles.Remove(role);
}
