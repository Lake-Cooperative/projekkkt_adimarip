using Adimarip.Application.Abstractions;

namespace Adimarip.Application.Access;

public sealed class AccessResolver(IUserRepository users, IRoleSnapshotProvider roles)
{
    public async Task<UserAccess?> ResolveAsync(string username, CancellationToken ct)
    {
        var user = await users.FindAsync(username, ct);
        if (user is null) return null;
        var role = await roles.GetAsync(user.RoleName, ct);
        if (role is null) return null;
        return new UserAccess(user.Username, user.DisplayName, role.Name, role.DisplayName, role.Permissions, user.RecipientId);
    }
}
