using System.Text.RegularExpressions;
using Adimarip.Domain.Common;

namespace Adimarip.Domain.Access;

/// <summary>Учётная запись. Пароль хранится только в виде хеша.</summary>
public sealed partial class AppUser
{
    public const int MinPasswordLength = 10;

    private AppUser() { } // для EF Core

    public string Username { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public string RoleName { get; private set; } = null!;
    public int? RecipientId { get; private set; }
    public string PasswordHash { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    public static bool IsValidUsername(string? username) => username is not null && UsernamePattern().IsMatch(username);

    public static Result<AppUser> Create(string? username, string? displayName, Role role, int? recipientId,
        string passwordHash, DateTimeOffset now)
    {
        var cleanUsername = TextRules.Clean(username);
        var cleanDisplayName = TextRules.Clean(displayName);
        if (!IsValidUsername(cleanUsername) || !TextRules.HasLength(cleanDisplayName, 2, 200))
            return Error.Validation("Проверьте логин, ФИО, роль и пароль (не менее 10 символов)");
        if (role.RequiresRecipientBinding && recipientId is null)
            return Error.Validation("Для этой роли учётная запись должна быть привязана к адресату");
        return new AppUser
        {
            Username = cleanUsername,
            DisplayName = cleanDisplayName,
            RoleName = role.Name,
            RecipientId = recipientId,
            PasswordHash = passwordHash,
            CreatedAt = now
        };
    }

    public Result AssignRole(Role role)
    {
        if (role.RequiresRecipientBinding && RecipientId is null)
            return Error.Conflict("Роль видит только свои данные, а учётная запись не привязана к адресату");
        RoleName = role.Name;
        return Result.Success();
    }

    [GeneratedRegex("^[a-zA-Z0-9_.-]{3,40}$")]
    private static partial Regex UsernamePattern();
}
