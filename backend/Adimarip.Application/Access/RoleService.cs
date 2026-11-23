using Adimarip.Application.Abstractions;
using Adimarip.Domain.Access;
using Adimarip.Domain.Common;

namespace Adimarip.Application.Access;

/// <summary>Управление кастомными ролями.</summary>
public sealed class RoleService(
    IRoleRepository roles,
    IUserRepository users,
    IRoleSnapshotProvider snapshots,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public IReadOnlyList<PermissionItem> Catalog() =>
        PermissionCatalog.All.Select(p => new PermissionItem(p.Permission.ToString(), p.Group, p.Description)).ToArray();

    public async Task<IReadOnlyList<RoleItem>> ListAsync(CancellationToken ct)
    {
        var all = await roles.ListAsync(ct);
        var usersByRole = await users.CountByRoleAsync(ct);
        return all
            .OrderByDescending(r => r.IsSystem).ThenBy(r => r.DisplayName)
            .Select(role => ToItem(role, usersByRole.GetValueOrDefault(role.Name)))
            .ToArray();
    }

    public async Task<Result<RoleItem>> CreateAsync(RoleInput input, CancellationToken ct)
    {
        var created = Role.CreateCustom(input.Name, input.DisplayName, input.Permissions, clock.UtcNow);
        if (created.IsFailure) return created.Error!;
        if (await roles.ExistsAsync(created.Value.Name, ct))
            return Error.Conflict("Роль с таким кодом уже существует");

        roles.Add(created.Value);
        return await SaveAsync(created.Value, usersCount: 0, ct);
    }

    public async Task<Result<RoleItem>> UpdateAsync(UserAccess actor, string name, RoleInput input, CancellationToken ct)
    {
        var role = await roles.GetAsync(name, ct);
        if (role is null) return Error.NotFound("Роль не найдена");

        var updated = role.Update(input.DisplayName, input.Permissions);
        if (updated.IsFailure) return updated.Error!;
        if (actor.RoleName == role.Name && !role.Has(Permission.RolesManage))
            return Error.Conflict("Нельзя убрать право управления ролями у собственной роли");
        if (role.RequiresRecipientBinding && await users.CountWithRoleAsync(role.Name, withoutRecipientOnly: true, ct) > 0)
            return Error.Conflict("У роли есть пользователи без привязки к адресату — область «только свои» недоступна");

        var usersCount = await users.CountWithRoleAsync(role.Name, withoutRecipientOnly: false, ct);
        return await SaveAsync(role, usersCount, ct);
    }

    public async Task<Result> DeleteAsync(string name, CancellationToken ct)
    {
        var role = await roles.GetAsync(name, ct);
        if (role is null) return Error.NotFound("Роль не найдена");
        if (role.IsSystem) return Error.Conflict("Системную роль удалить нельзя");
        if (await users.CountWithRoleAsync(role.Name, withoutRecipientOnly: false, ct) > 0)
            return Error.Conflict("Роль назначена пользователям — сначала смените им роль");

        roles.Remove(role);
        var saved = await unitOfWork.SaveAsync("Роль используется — удаление невозможно", ct);
        if (saved.IsSuccess) snapshots.Invalidate(role.Name);
        return saved;
    }

    private async Task<Result<RoleItem>> SaveAsync(Role role, int usersCount, CancellationToken ct)
    {
        var saved = await unitOfWork.SaveAsync("Роль изменена параллельно или код уже занят", ct);
        if (saved.IsFailure) return saved.Error!;
        snapshots.Invalidate(role.Name);
        return ToItem(role, usersCount);
    }

    private static RoleItem ToItem(Role role, int usersCount) => new(
        role.Name, role.DisplayName, role.IsSystem, Role.ScopeOf(role.PermissionSet).ToString(),
        role.RequiresRecipientBinding, role.Permissions.Select(p => p.Permission.ToString()).Order().ToArray(), usersCount);
}
