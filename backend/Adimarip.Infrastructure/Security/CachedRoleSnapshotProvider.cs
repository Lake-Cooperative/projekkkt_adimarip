using Adimarip.Application.Abstractions;
using Adimarip.Application.Access;
using Microsoft.Extensions.Caching.Memory;

namespace Adimarip.Infrastructure.Security;

/// <summary>
/// Кэширующий заместитель (Proxy) для ролей: права проверяются на каждом запросе,
/// а читать их каждый раз из БД незачем. После изменения роли кэш сбрасывается.
/// </summary>
internal sealed class CachedRoleSnapshotProvider(IRoleRepository roles, IMemoryCache cache) : IRoleSnapshotProvider
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(30);

    public async Task<RoleSnapshot?> GetAsync(string roleName, CancellationToken ct)
    {
        if (cache.TryGetValue(Key(roleName), out RoleSnapshot? cached)) return cached;
        var role = await roles.GetAsync(roleName, ct);
        if (role is null) return null;
        var snapshot = RoleSnapshot.From(role);
        cache.Set(Key(roleName), snapshot, Lifetime);
        return snapshot;
    }

    public void Invalidate(string roleName) => cache.Remove(Key(roleName));

    private static string Key(string roleName) => $"role:{roleName}";
}
