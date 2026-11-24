using Adimarip.Application.Abstractions;
using Adimarip.Domain.Common;
using Adimarip.Domain.Directory;

namespace Adimarip.Application.Access;

public sealed class AuthService(
    IUserRepository users,
    IRecipientRepository recipients,
    IPasswordHasher hasher,
    AccessResolver accessResolver)
{
    public async Task<Result<LoginResult>> LoginAsync(LoginInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Username) || string.IsNullOrEmpty(input.Password))
            return Error.Validation("Укажите логин и пароль");

        var user = await users.FindForLoginAsync(input.Username.Trim(), ct);
        if (user is null || !hasher.Verify(user.PasswordHash, input.Password))
            return Error.Unauthorized("Неверный логин или пароль");

        var access = await accessResolver.ResolveAsync(user.Username, ct);
        if (access is null)
            return Error.Unauthorized("Роль учётной записи не найдена");

        var recipientName = user.RecipientId is int id ? await recipients.GetFullNameAsync(id, ct) : null;
        return new LoginResult(access, Recipient.SurnameOf(recipientName ?? user.DisplayName));
    }
}
