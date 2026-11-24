using Adimarip.Application.Abstractions;
using Adimarip.Application.Directory;
using Microsoft.EntityFrameworkCore;

namespace Adimarip.Infrastructure.Persistence.Repositories;

internal sealed class OfficeRepository(AdimaripDbContext db) : IOfficeRepository
{
    public async Task<IReadOnlyList<OfficeItem>> ListAsync(int? onlyWithNoticesOfRecipient, CancellationToken ct)
    {
        var query = db.Offices.AsNoTracking();
        if (onlyWithNoticesOfRecipient is { } recipientId)
            query = query.Where(o => db.Notices.Any(n => n.OfficeId == o.Id && n.RecipientId == recipientId));
        return await query.OrderBy(o => o.Name).Select(o => new OfficeItem(o.Id, o.Name)).ToListAsync(ct);
    }

    public Task<bool> ExistsAsync(int id, CancellationToken ct) => db.Offices.AnyAsync(o => o.Id == id, ct);
}
