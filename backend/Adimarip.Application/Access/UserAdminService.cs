using Adimarip.Application.Abstractions;
using Adimarip.Domain.Access;
using Adimarip.Domain.Common;
using Adimarip.Domain.Directory;

namespace Adimarip.Application.Access;

/// <summary>
/// Учётные записи. Если роль видит «только свои» данные (как Служащий), то вместе с
/// учётной записью в одной транзакции создаётся адресат, и его ID становится ID работника.
/// </summary>
public sealed class UserAdminService(
    IUserRepository users,
    IRoleRepository roles,
    IRecipientRepository recipients,
    IOfficeRepository offices,
    IPasswordHasher hasher,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    private const string InvalidAccountMessage = "Проверьте логин, ФИО, роль и пароль (не менее 10 символов)";

    public Task<IReadOnlyList<UserListItem>> ListAsync(CancellationToken ct) => users.ListAsync(ct);

    public async Task<Result<CreatedAccount>> CreateAsync(CreateAccountInput input, CancellationToken ct)
    {
        var username = TextRules.Clean(input.Username);
        var role = string.IsNullOrWhiteSpace(input.Role) ? null : await roles.GetAsync(input.Role, ct);
        if (!AppUser.IsValidUsername(username) || !TextRules.HasLength(TextRules.Clean(input.DisplayName), 2, 200) ||
            role is null || input.Password is not { Length: >= AppUser.MinPasswordLength } password)
            return Error.Validation(InvalidAccountMessage);
        if (await users.FindForLoginAsync(username, ct) is not null)
            return Error.Conflict("Такой логин уже существует");

        var passwordHash = hasher.Hash(password);
        return role.RequiresRecipientBinding
            ? await CreateWithRecipientAsync(input, username, role, passwordHash, ct)
            : await CreateAccountOnlyAsync(input, username, role, passwordHash, ct);
    }

    public async Task<Result<UserListItem>> ChangeRoleAsync(UserAccess actor, string username, ChangeRoleInput input, CancellationToken ct)
    {
        var user = await users.FindAsync(username, ct);
        if (user is null) return Error.NotFound("Учётная запись не найдена");
        var role = string.IsNullOrWhiteSpace(input.Role) ? null : await roles.GetAsync(input.Role, ct);
        if (role is null) return Error.Validation("Роль не найдена");
        if (user.Username == actor.Username && !role.Has(Permission.UsersManage))
            return Error.Conflict("Нельзя лишить себя права управления пользователями");

        var assigned = user.AssignRole(role);
        if (assigned.IsFailure) return assigned.Error!;
        var saved = await unitOfWork.SaveAsync("Учётная запись изменена параллельно", ct);
        if (saved.IsFailure) return saved.Error!;
        return new UserListItem(user.Username, user.DisplayName, user.RoleName, user.RecipientId, user.CreatedAt);
    }

    private async Task<Result<CreatedAccount>> CreateAccountOnlyAsync(CreateAccountInput input, string username,
        Role role, string passwordHash, CancellationToken ct)
    {
        var user = AppUser.Create(username, input.DisplayName, role, null, passwordHash, clock.UtcNow);
        if (user.IsFailure) return user.Error!;
        users.Add(user.Value);
        var saved = await unitOfWork.SaveAsync("Такой логин уже существует", ct);
        if (saved.IsFailure) return saved.Error!;
        return new CreatedAccount(user.Value.Username, role.Name, null);
    }

    private async Task<Result<CreatedAccount>> CreateWithRecipientAsync(CreateAccountInput input, string username,
        Role role, string passwordHash, CancellationToken ct)
    {
        var recipient = await PrepareRecipientAsync(input, username, ct);
        if (recipient.IsFailure) return recipient.Error!;

        return await unitOfWork.InTransactionAsync<CreatedAccount>(async () =>
        {
            recipients.Add(recipient.Value);
            await unitOfWork.SaveChangesAsync(ct); // нужен Id адресата для привязки
            var user = AppUser.Create(username, input.DisplayName, role, recipient.Value.Id, passwordHash, clock.UtcNow);
            if (user.IsFailure) return user.Error!;
            users.Add(user.Value);
            await unitOfWork.SaveChangesAsync(ct);
            return new CreatedAccount(username, role.Name, recipient.Value.Id);
        }, TransactionMode.ReadCommitted, "Не удалось создать работника: идентификатор или логин уже заняты", ct);
    }

    /// <summary>Карточка адресата для работника: ID адресата станет ID работника.</summary>
    private async Task<Result<Recipient>> PrepareRecipientAsync(CreateAccountInput input, string username, CancellationToken ct)
    {
        if (input.DateOfBirth is null || input.DateOfBirth > clock.Today ||
            !TextRules.FitsOptional(input.Email, 320) || !TextRules.FitsOptional(input.Phone, 50))
            return Error.Validation("Укажите корректную дату рождения и контакты работника");
        if (input.OfficeId is int officeId && !await offices.ExistsAsync(officeId, ct))
            return Error.NotFound("Подразделение не найдено");

        var identifier = string.IsNullOrWhiteSpace(input.ExternalIdentifier)
            ? $"EMP-{username.ToUpperInvariant()}"
            : input.ExternalIdentifier.Trim();
        var profile = new RecipientProfile(identifier, input.DisplayName, input.DateOfBirth, input.Email, input.Phone,
            input.OfficeId, new AddressData(null, null, input.City, null, null, null));
        var recipient = Recipient.Create(profile, clock.Today);
        if (recipient.IsFailure) return recipient;
        if (await recipients.IdentifierExistsAsync(recipient.Value.ExternalIdentifier, ct))
            return Error.Conflict("Идентификатор адресата уже существует");
        return recipient;
    }
}
