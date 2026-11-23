using Adimarip.Application.Abstractions;
using Adimarip.Application.Directory;
using Adimarip.Domain.Broadcasts;
using Adimarip.Domain.Directory;
using Microsoft.EntityFrameworkCore;

namespace Adimarip.Infrastructure.Persistence.Repositories;

internal sealed class RecipientRepository(AdimaripDbContext db) : IRecipientRepository
{
    public async Task<IReadOnlyList<RecipientListItem>> SearchActiveAsync(string? term, int? onlyRecipientId, int take,
            CancellationToken ct)
    {
        var query = Active();
        if (onlyRecipientId is { } id) query = query.Where(r => r.Id == id);
        if (term is not null)
        {
            var pattern = LikePattern.Contains(term);
            query = query.Where(r => EF.Functions.ILike(r.FullName, pattern) || EF.Functions.ILike(r.ExternalIdentifier, pattern));
        }
        return await query
            .OrderBy(r => r.FullName)
            .Take(take)
            .Select(r => new RecipientListItem(r.Id, r.ExternalIdentifier, r.FullName, r.DateOfBirth, r.Email, r.Phone,
                r.OfficeId, db.Offices.Where(o => o.Id == r.OfficeId).Select(o => o.Name).FirstOrDefault()))
            .ToListAsync(ct);
    }

    public Task<Recipient?> GetForUpdateAsync(int id, CancellationToken ct) =>
        db.Recipients.Include(r => r.ArchiveRecord).FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<RecipientState> GetStateAsync(int id, CancellationToken ct)
    {
        var archived = await db.Recipients.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => (bool?)(r.ArchiveRecord != null))
            .FirstOrDefaultAsync(ct);
        return archived switch
        {
            null => RecipientState.Missing,
            true => RecipientState.Archived,
            false => RecipientState.Active
        };
    }

    public Task<string?> GetFullNameAsync(int id, CancellationToken ct) =>
        db.Recipients.AsNoTracking().Where(r => r.Id == id).Select(r => r.FullName).FirstOrDefaultAsync(ct);

    public Task<bool> IdentifierExistsAsync(string identifier, CancellationToken ct) =>
        db.Recipients.AnyAsync(r => r.ExternalIdentifier == identifier, ct);

    public Task<int> CountAudienceAsync(BroadcastAudience audience, CancellationToken ct) =>
        Audience(audience).CountAsync(ct);

    public async Task<IReadOnlyList<RecipientContacts>> GetAudienceAsync(BroadcastAudience audience, CancellationToken ct) =>
        await Audience(audience)
            .OrderBy(r => r.Id)
            .Select(r => new RecipientContacts(r.Id, r.FullName, r.Email, r.Phone))
            .ToListAsync(ct);

    public void Add(Recipient recipient) => db.Recipients.Add(recipient);

    private IQueryable<Recipient> Active() => db.Recipients.AsNoTracking().Where(r => r.ArchiveRecord == null);

    /// <summary>Перевод спецификации аудитории в SQL-условие.</summary>
    private IQueryable<Recipient> Audience(BroadcastAudience audience)
    {
        var offices = audience.OfficeIds.ToArray();
        var recipients = audience.RecipientIds.ToArray();
        return audience.Kind switch
        {
            AudienceKind.Offices => Active().Where(r => r.OfficeId != null && offices.Contains(r.OfficeId.Value)),
            AudienceKind.Recipients => Active().Where(r => recipients.Contains(r.Id)),
            _ => Active()
        };
    }
}
