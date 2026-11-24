using Adimarip.Application.Abstractions;
using Adimarip.Application.Access;
using Adimarip.Domain.Common;

namespace Adimarip.Application.Notices;

/// <summary>Регистрация обращений по оповещению.</summary>
public sealed class AppealService(INoticeRepository notices, IUnitOfWork unitOfWork, IClock clock)
{
    public async Task<Result<AppealCreated>> FileAsync(UserAccess access, int noticeId, AppealInput input, CancellationToken ct)
    {
        var notice = await notices.GetForUpdateAsync(noticeId, ownerRecipientId: null, ct);
        if (notice is null) return Error.NotFound("Оповещение не найдено");

        var appeal = notice.FileAppeal(input.Type, input.Text, access.Username, clock.UtcNow);
        if (appeal.IsFailure) return appeal.Error!;
        await unitOfWork.SaveChangesAsync(ct);
        return new AppealCreated(appeal.Value.Id, appeal.Value.Status);
    }
}
