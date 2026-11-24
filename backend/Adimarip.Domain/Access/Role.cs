using System.Text.RegularExpressions;
using Adimarip.Domain.Common;

namespace Adimarip.Domain.Access;

/// <summary>Область данных, которую видит роль.</summary>
public enum DataScope
{
    None,
    Own,
    All
}

/// <summary>Роль = именованный набор прав. Системные роли менять и удалять нельзя.</summary>
public sealed partial class Role
{
    private readonly List<RolePermission> _permissions = new();

    private Role() { } // для EF Core

    public string Name { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public bool IsSystem { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyCollection<RolePermission> Permissions => _permissions;

    public IReadOnlySet<Permission> PermissionSet => _permissions.Select(p => p.Permission).ToHashSet();

    public bool Has(Permission permission) => _permissions.Any(p => p.Permission == permission);

    /// <summary>Роль видит только «свои» данные — значит, учётной записи нужна привязка к адресату.</summary>
    public bool RequiresRecipientBinding => ScopeOf(PermissionSet) == DataScope.Own;

    public static DataScope ScopeOf(IReadOnlySet<Permission> permissions) =>
        permissions.Contains(Permission.NoticesReadAll) ? DataScope.All
        : permissions.Contains(Permission.NoticesReadOwn) ? DataScope.Own
        : DataScope.None;

    public static Result<Role> CreateCustom(string? name, string? displayName, IEnumerable<string>? permissions, DateTimeOffset now)
    {
        var cleanName = TextRules.Clean(name);
        if (!NamePattern().IsMatch(cleanName))
            return Error.Validation("Код роли: латиница, цифры и _, от 3 до 40 символов, начинается с буквы");
        var role = new Role { Name = cleanName, IsSystem = false, CreatedAt = now };
        var updated = role.ApplyChanges(displayName, permissions);
        return updated.IsSuccess ? role : updated.Error!;
    }

    internal static Role CreateSystem(string name, string displayName, IEnumerable<Permission> permissions)
    {
        var role = new Role { Name = name, DisplayName = displayName, IsSystem = true };
        role._permissions.AddRange(permissions.Select(p => new RolePermission(name, p)));
        return role;
    }

    public Result Update(string? displayName, IEnumerable<string>? permissions)
    {
        if (IsSystem)
            return Error.Conflict("Системную роль изменить нельзя — создайте кастомную роль на её основе");
        return ApplyChanges(displayName, permissions);
    }

    private Result ApplyChanges(string? displayName, IEnumerable<string>? permissions)
    {
        var cleanDisplayName = TextRules.Clean(displayName);
        if (!TextRules.HasLength(cleanDisplayName, 2, 100))
            return Error.Validation("Название роли: от 2 до 100 символов");

        var parsed = new HashSet<Permission>();
        foreach (var raw in permissions ?? [])
        {
            if (!EnumNames.TryParse<Permission>(raw, out var permission))
                return Error.Validation($"Неизвестное право: {raw}");
            parsed.Add(permission);
        }
        if (parsed.Count == 0)
            return Error.Validation("Выберите хотя бы одно право");
        if (parsed.Contains(Permission.NoticesReadAll) && parsed.Contains(Permission.NoticesReadOwn))
            parsed.Remove(Permission.NoticesReadOwn); // «все» включает «свои»

        DisplayName = cleanDisplayName;
        // Меняем только разницу, чтобы EF не удалял и не вставлял одинаковые строки.
        _permissions.RemoveAll(p => !parsed.Contains(p.Permission));
        foreach (var permission in parsed.Where(p => !Has(p)))
            _permissions.Add(new RolePermission(Name, permission));
        return Result.Success();
    }

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_]{2,39}$")]
    private static partial Regex NamePattern();
}
