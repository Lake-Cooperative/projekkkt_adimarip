namespace Adimarip.Domain.Access;

/// <summary>Строка связи «роль — право».</summary>
public sealed class RolePermission
{
    private RolePermission() { } // для EF Core

    internal RolePermission(string roleName, Permission permission)
    {
        RoleName = roleName;
        Permission = permission;
    }

    public string RoleName { get; private set; } = null!;
    public Permission Permission { get; private set; }
}
