using Adimarip.Application.Abstractions;
using Adimarip.Application.Access;
using Adimarip.Domain.Common;
using Adimarip.Domain.Notices;

namespace Adimarip.Application.Notices;

/// <summary>Сценарии реестра: поиск, карточка, регистрация, смена статуса, подтверждение получения.</summary>
public sealed class NoticeService(
    INoticeRepository notices,
    IRecipientRepository recipients,
    IOfficeRepository offices,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    private const int DefaultPageSize = 15;
    private const int MaxPageSize = 100;
    private const int MaxSearchLength = 100;
    private const string ConcurrentChangeMessage = "Запись изменена параллельно. Обновите карточку";

    public async Task<Result<PagedResult<NoticeListItem>>> SearchAsync(UserAccess access, NoticeSearchInput input, CancellationToken ct)
    {
        var owner = access.RecipientFilter();
        if (owner.IsFailure) return owner.Error!;
        var criteria = ParseSearch(input, owner.Value);
        if (criteria.IsFailure) return criteria.Error!;

        // Неизвестный статус в фильтре — пустой результат, а не ошибка (как в исходной версии).
        if (criteria.Value is null)
            return new PagedResult<NoticeListItem>([], 0, input.Page ?? 1, input.PageSize ?? DefaultPageSize);
        return await notices.SearchAsync(criteria.Value, ct);
    }

    public async Task<Result<NoticeDetail>> GetAsync(UserAccess access, int id, CancellationToken ct)
    {
        var owner = access.RecipientFilter();
        if (owner.IsFailure) return owner.Error!;
        var notice = await notices.GetDetailsAsync(id, owner.Value, ct);
        return notice is null ? NotFound() : NoticeDetailMapper.Map(notice, access);
    }

    public async Task<Result<NoticeCreated>> CreateAsync(UserAccess access, CreateNoticeInput input, CancellationToken ct)
    {
        var registered = Notice.Register(input.ToDraft(), access.Username, clock.UtcNow);
        if (registered.IsFailure) return registered.Error!;
        var notice = registered.Value;

        var references = await CheckReferencesAsync(notice, ct);
        if (references.IsFailure) return references.Error!;

        notices.Add(notice);
        // Оповещение, начальная история и аудит сохраняются одной транзакцией.
        var saved = await unitOfWork.SaveAsync("Номер уже существует или данные не прошли ограничения БД", ct);
        if (saved.IsFailure) return saved.Error!;
        return new NoticeCreated(notice.Id, notice.Number, notice.CurrentStatus.ToString());
    }

    public Task<Result<NoticeStatusChanged>> ChangeStatusAsync(UserAccess access, int id, ChangeStatusInput input, CancellationToken ct)
    {
        var raw = TextRules.Clean(input.Status);
        if (!EnumNames.TryParse<NoticeStatus>(raw, out var target))
            return Task.FromResult<Result<NoticeStatusChanged>>(Error.Validation($"Неизвестный статус: {raw}"));

        return ChangeConcurrentlyAsync(id, ownerRecipientId: null,
            notice => notice.ChangeStatus(target, access.Username, input.Comment, clock.UtcNow), ct);
    }

    /// <summary>Работник подтверждает, что ознакомился со своим оповещением.</summary>
    public Task<Result<NoticeStatusChanged>> AcknowledgeAsync(UserAccess access, int id, CancellationToken ct)
    {
        if (access.RecipientId is not > 0)
            return Task.FromResult<Result<NoticeStatusChanged>>(Error.Forbidden("Подтвердить можно только собственное оповещение"));

        return ChangeConcurrentlyAsync(id, access.RecipientId,
            notice => notice.Acknowledge(access.Username, clock.UtcNow), ct);
    }

    private static Result<NoticeSearch?> ParseSearch(NoticeSearchInput input, int? ownerRecipientId)
    {
        var page = input.Page ?? 1;
        var size = input.PageSize ?? DefaultPageSize;
        var search = TextRules.CleanOptional(input.Search);
        var pagingIsValid = page >= 1 && size is >= 1 and <= MaxPageSize && page <= int.MaxValue / size;
        if (!pagingIsValid || !TextRules.FitsOptional(search, MaxSearchLength) || input.OfficeId < 1)
            return Error.Validation("Некорректные параметры поиска или пагинации");

        NoticeStatus? status = null;
        if (!string.IsNullOrWhiteSpace(input.Status))
        {
            if (!EnumNames.TryParse<NoticeStatus>(input.Status, out var parsed))
                return Result<NoticeSearch?>.Success(null);
            status = parsed;
        }
        return new NoticeSearch(ownerRecipientId, status, input.OfficeId, search, new PageRequest(page, size));
    }

    private async Task<Result> CheckReferencesAsync(Notice notice, CancellationToken ct)
    {
        var recipientState = await recipients.GetStateAsync(notice.RecipientId, ct);
        if (recipientState == RecipientState.Missing || !await offices.ExistsAsync(notice.OfficeId, ct))
            return Error.NotFound("Адресат или подразделение не найдены");
        if (recipientState == RecipientState.Archived)
            return Error.Conflict("Нельзя выписать оповещение списанному адресату");
        if (await notices.NumberExistsAsync(notice.Number, ct))
            return Error.Conflict("Номер оповещения занят");
        return Result.Success();
    }

    /// <summary>Изменение одной записи в Serializable-транзакции; параллельная правка → 409.</summary>
    private Task<Result<NoticeStatusChanged>> ChangeConcurrentlyAsync(int id, int? ownerRecipientId,
        Func<Notice, Result> change, CancellationToken ct) =>
        unitOfWork.InTransactionAsync<NoticeStatusChanged>(async () =>
        {
            var notice = await notices.GetForUpdateAsync(id, ownerRecipientId, ct);
            if (notice is null) return NotFound();
            var changed = change(notice);
            if (changed.IsFailure) return changed.Error!;
            await unitOfWork.SaveChangesAsync(ct);
            return new NoticeStatusChanged(notice.Id, notice.CurrentStatus.ToString());
        }, TransactionMode.Serializable, ConcurrentChangeMessage, ct);

    private static Error NotFound() => Error.NotFound("Оповещение не найдено");
}
