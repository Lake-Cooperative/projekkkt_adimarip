namespace Adimarip.Application.Access;

public sealed record LoginInput(string? Username, string? Password);

/// <summary>Успешный вход: права и фамилия нужны интерфейсу для шапки и меню.</summary>
public sealed record LoginResult(UserAccess Access, string Surname);

public sealed record CreateAccountInput(
    string? Username,
    string? DisplayName,
    string? Role,
    int? RecipientId,
    string? Password,
    DateOnly? DateOfBirth = null,
    string? Email = null,
    string? Phone = null,
    string? City = null,
    string? ExternalIdentifier = null,
    int? OfficeId = null);

public sealed record CreatedAccount(string Username, string Role, int? RecipientId);

public sealed record ChangeRoleInput(string? Role);

public sealed record UserListItem(string Username, string DisplayName, string Role, int? RecipientId, DateTimeOffset CreatedAt);

public sealed record RoleInput(string? Name, string? DisplayName, IReadOnlyList<string>? Permissions);

public sealed record RoleItem(string Name, string DisplayName, bool IsSystem, string Scope,
    bool RequiresRecipientBinding, IReadOnlyList<string> Permissions, int UsersCount);

public sealed record PermissionItem(string Code, string Group, string Description);
