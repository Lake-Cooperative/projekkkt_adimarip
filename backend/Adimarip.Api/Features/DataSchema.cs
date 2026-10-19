using Adimarip.Data;
using Microsoft.EntityFrameworkCore;

namespace Adimarip.Api.Features;

public static class DataSchema
{
    public static async Task ApplyAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdimaripDbContext>();
        // All schema changes, including AppUsers, now have a versioned migration.
        // On an existing database EF applies only missing versions.
        await db.Database.MigrateAsync(ct);
    }
}
