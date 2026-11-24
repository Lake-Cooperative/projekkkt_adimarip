using Adimarip.Domain.Access;

namespace Adimarip.Application.Access;

/// <summary>Неизменяемый снимок роли для проверки прав на каждый запрос.</summary>
public sealed record RoleSnapshot(string Name, string DisplayName, IReadOnlySet<Permission> Permissions)
{
    public static RoleSnapshot From(Role role) => new(role.Name, role.DisplayName, role.PermissionSet);
}

/// <summary>Источник снимков ролей. Реализация в Infrastructure кэширует их (Proxy над репозиторием).</summary>
public interface IRoleSnapshotProvider
{
    Task<RoleSnapshot?> GetAsync(string roleName, CancellationToken ct);
    void Invalidate(string roleName);
}
