using Adimarip.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Adimarip.Infrastructure.Startup;

/// <summary>Применяет недостающие версионные миграции (на существующей БД — только новые).</summary>
internal sealed class DatabaseMigrationTask(AdimaripDbContext db) : IStartupTask
{
    public Task ExecuteAsync(CancellationToken ct) => db.Database.MigrateAsync(ct);
}
