using Adimarip.Application.Abstractions;
using Adimarip.Domain.Access;

namespace Adimarip.Infrastructure.Startup;

/// <summary>Общая логика «создать учётную запись, если её ещё нет» — пароль существующей не сбрасывается.</summary>
internal sealed class AccountSeeding(IUserRepository users, IRoleRepository roles, IPasswordHasher hasher, IClock clock)
{
    public async Task EnsureAsync(string username, string displayName, string roleName, int? recipientId,
        string password, CancellationToken ct)
    {
        if (await users.FindForLoginAsync(username, ct) is not null) return;
        var role = await roles.GetAsync(roleName, ct)
                   ?? throw new InvalidOperationException($"Role {roleName} is missing; check migrations.");
        var user = AppUser.Create(username, displayName, role, recipientId, hasher.Hash(password), clock.UtcNow);
        if (user.IsFailure) throw new InvalidOperationException($"Cannot seed account {username}: {user.Error!.Message}");
        users.Add(user.Value);
    }
}
