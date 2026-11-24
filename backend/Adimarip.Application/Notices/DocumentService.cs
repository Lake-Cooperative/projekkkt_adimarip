using Adimarip.Application.Abstractions;
using Adimarip.Application.Access;
using Adimarip.Domain.Common;

namespace Adimarip.Application.Notices;

/// <summary>Ссылки на сопроводительные документы (хранятся только метаданные).</summary>
public sealed class DocumentService(INoticeRepository notices, IUnitOfWork unitOfWork, IClock clock)
{
    public async Task<Result<DocumentCreated>> AttachAsync(UserAccess access, int noticeId, DocumentInput input, CancellationToken ct)
    {
        // Работник с областью «только свои» прикрепляет документ лишь к своему оповещению: чужое — 404.
        var owner = access.RecipientFilter();
        if (owner.IsFailure) return owner.Error!;
        var notice = await notices.GetForUpdateAsync(noticeId, owner.Value, ct);
        if (notice is null) return Error.NotFound("Оповещение не найдено");

        var document = notice.AttachDocument(input.Name, input.MimeType, input.StorageUri, access.Username, clock.UtcNow);
        if (document.IsFailure) return document.Error!;
        await unitOfWork.SaveChangesAsync(ct);
        return new DocumentCreated(document.Value.Id, document.Value.Name);
    }
}
