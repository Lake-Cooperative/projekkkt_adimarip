using Adimarip.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Adimarip.Infrastructure.Persistence;

internal sealed class HealthProbe(AdimaripDbContext db) : IHealthProbe
{
    public Task<bool> IsDatabaseAvailableAsync(CancellationToken ct) => db.Database.CanConnectAsync(ct);
}
