using Adimarip.Data;
using Microsoft.EntityFrameworkCore;

namespace Adimarip.Api.Features;

public static class DataSchema
{
    public static async Task ApplyAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdimaripDbContext>();
        // Requires a fresh or migration-managed demo database. No EnsureCreated/Migrate mixing.
        await db.Database.MigrateAsync(ct);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "AppUsers" (
                "Username" varchar(100) PRIMARY KEY,
                "DisplayName" varchar(200) NOT NULL,
                "PasswordHash" text NOT NULL,
                "Role" varchar(40) NOT NULL CHECK ("Role" IN ('Operator','Manager','Observer','AutomationEngineer')),
                "RecipientId" integer NULL REFERENCES "Recipients"("Id") ON DELETE SET NULL,
                "CreatedAt" timestamptz NOT NULL DEFAULT now()
            )
            """, ct);
    }
}
