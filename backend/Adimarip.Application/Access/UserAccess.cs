using Adimarip.Domain.Access;
using Adimarip.Domain.Common;

namespace Adimarip.Application.Access;

/// <summary>
/// Кто выполняет действие и что ему разрешено. Строится по данным БД, а не по
/// содержимому токена: изменение роли или привязки действует сразу.
/// </summary>
public sealed record UserAccess(
    string Username,
    string DisplayName,
    string RoleName,
    string RoleDisplayName,
    IReadOnlySet<Permission> Permissions,
    int? RecipientId)
{
    public bool Has(Permission permission) => Permissions.Contains(permission);

    public bool HasAny(params Permission[] permissions) => permissions.Any(Has);

    public DataScope Scope => Role.ScopeOf(Permissions);

    /// <summary>
    /// Фильтр «чьи записи видны»: null — все, число — только этого адресата.
    /// «Свои» без привязки к адресату — отказ (fail closed).
    /// </summary>
    public Result<int?> RecipientFilter() => Scope switch
    {
        DataScope.All => Result<int?>.Success(null),
        DataScope.Own when RecipientId is > 0 => Result<int?>.Success(RecipientId),
        _ => Error.Forbidden()
    };

    public IReadOnlyList<string> PermissionCodes => Permissions.Select(p => p.ToString()).Order().ToArray();
}
