using Adimarip.Application.Abstractions;
using Adimarip.Application.Access;
using Adimarip.Domain.Access;
using Adimarip.Domain.Common;
using Adimarip.Domain.Directory;

namespace Adimarip.Application.Directory;

/// <summary>Справочники: подразделения и адресаты.</summary>
public sealed class DirectoryService(
    IRecipientRepository recipients,
    IOfficeRepository offices,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    private const int MaxSearchResults = 100;

    public async Task<Result<IReadOnlyList<OfficeItem>>> ListOfficesAsync(UserAccess access, CancellationToken ct)
    {
        // Полный справочник нужен тем, кто работает со всем реестром, рассылками или учётными записями.
        if (access.HasAny(Permission.NoticesReadAll, Permission.BroadcastsSend, Permission.UsersManage))
            return Result<IReadOnlyList<OfficeItem>>.Success(await offices.ListAsync(null, ct));
        var owner = access.RecipientFilter();
        if (owner.IsFailure) return owner.Error!;
        return Result<IReadOnlyList<OfficeItem>>.Success(await offices.ListAsync(owner.Value, ct));
    }

    public async Task<Result<IReadOnlyList<RecipientListItem>>> SearchRecipientsAsync(UserAccess access, string? search,
            CancellationToken ct)
    {
        var term = TextRules.CleanOptional(search);
        if (term?.Length > 100) return Error.Validation("Строка поиска слишком длинная");

        int? onlyId = null;
        if (!access.Has(Permission.RecipientsRead))
        {
            // Без доступа к справочнику работник видит только собственную карточку.
            var owner = access.RecipientFilter();
            if (owner.IsFailure || owner.Value is null) return Error.Forbidden();
            onlyId = owner.Value;
        }
        return Result<IReadOnlyList<RecipientListItem>>.Success(
            await recipients.SearchActiveAsync(term, onlyId, MaxSearchResults, ct));
    }

    public async Task<Result<RecipientCreated>> CreateRecipientAsync(RecipientInput input, CancellationToken ct)
    {
        var created = Recipient.Create(new RecipientProfile(input.ExternalIdentifier, input.FullName, input.DateOfBirth,
            input.Email, input.Phone, input.OfficeId, input.Address?.ToData()), clock.Today);
        if (created.IsFailure) return created.Error!;
        var recipient = created.Value;
        if (input.OfficeId is int officeId && !await offices.ExistsAsync(officeId, ct))
            return Error.NotFound("Подразделение не найдено");
        if (await recipients.IdentifierExistsAsync(recipient.ExternalIdentifier, ct))
            return Error.Conflict("Идентификатор уже существует");

        recipients.Add(recipient);
        var saved = await unitOfWork.SaveAsync("Не удалось сохранить: идентификатор уже существует или данные некорректны", ct);
        if (saved.IsFailure) return saved.Error!;
        return new RecipientCreated(recipient.Id, recipient.FullName, recipient.ExternalIdentifier);
    }

    public async Task<Result<RecipientArchived>> ArchiveRecipientAsync(UserAccess access, int id, ArchiveRecipientInput input,
            CancellationToken ct)
    {
        var recipient = await recipients.GetForUpdateAsync(id, ct);
        if (recipient is null) return Error.NotFound("Адресат не найден");

        var archived = recipient.Archive(input.Reason, access.Username, clock.UtcNow);
        if (archived.IsFailure) return archived.Error!;
        var saved = await unitOfWork.SaveAsync("Адресат уже списан", ct);
        if (saved.IsFailure) return saved.Error!;
        var record = recipient.ArchiveRecord!;
        return new RecipientArchived(id, record.ArchivedAt, record.Reason);
    }
}
