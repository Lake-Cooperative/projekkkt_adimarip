using Adimarip.Application.Abstractions;
using Adimarip.Domain.Broadcasts;
using Microsoft.EntityFrameworkCore;

namespace Adimarip.Infrastructure.Persistence.Repositories;

internal sealed class BroadcastRepository(AdimaripDbContext db) : IBroadcastRepository
{
    public Task<Broadcast?> GetForUpdateAsync(int id, CancellationToken ct) =>
        db.Broadcasts.FirstOrDefaultAsync(b => b.Id == id, ct);

    public Task<Broadcast?> GetAsync(int id, CancellationToken ct) =>
        db.Broadcasts.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<IReadOnlyList<Broadcast>> ListRecentAsync(int take, CancellationToken ct) =>
        await db.Broadcasts.AsNoTracking().OrderByDescending(b => b.Id).Take(take).ToListAsync(ct);

    public async Task<IReadOnlyList<(int Id, BroadcastSeverity Severity)>> GetUnfinishedAsync(CancellationToken ct)
    {
        var rows = await db.Broadcasts.AsNoTracking()
            .Where(b => b.Status == BroadcastStatus.Queued || b.Status == BroadcastStatus.Dispatching)
            .OrderBy(b => b.Id)
            .Select(b => new { b.Id, b.Severity })
            .ToListAsync(ct);
        return rows.Select(r => (r.Id, r.Severity)).ToArray();
    }

    public void Add(Broadcast broadcast) => db.Broadcasts.Add(broadcast);
}
