using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Adimarip.Data.Migrations;

[DbContext(typeof(AdimaripDbContext))]
[Migration("202610190005_AppUsers")]
public sealed class AppUsers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Historically the table was created by startup SQL outside migrations.
        // IF NOT EXISTS upgrades existing demonstration volumes without dropping accounts.
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS "AppUsers" (
                "Username" varchar(100) PRIMARY KEY,
                "DisplayName" varchar(200) NOT NULL,
                "PasswordHash" text NOT NULL,
                "Role" varchar(40) NOT NULL CHECK ("Role" IN ('Operator','Manager','Observer','AutomationEngineer')),
                "RecipientId" integer NULL REFERENCES "Recipients"("Id") ON DELETE RESTRICT,
                "CreatedAt" timestamptz NOT NULL DEFAULT now(),
                CONSTRAINT "CK_AppUsers_Observer_Recipient" CHECK ("Role" <> 'Observer' OR "RecipientId" IS NOT NULL)
            );
            CREATE INDEX IF NOT EXISTS "IX_AppUsers_Role" ON "AppUsers" ("Role");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Safe rollback preserves accounts; drop manually only after an explicit backup.
        migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_AppUsers_Role\";");
    }
}
