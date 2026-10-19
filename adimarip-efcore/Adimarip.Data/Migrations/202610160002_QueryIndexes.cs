using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Adimarip.Data.Migrations;

[DbContext(typeof(AdimaripDbContext))]
[Migration("202610160002_QueryIndexes")]
public sealed class QueryIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex("IX_Notices_Status_Created", "Notices",
            new[] { "CurrentStatus", "CreatedAt" });
        migrationBuilder.CreateIndex("IX_Notices_Office_Status", "Notices",
            new[] { "OfficeId", "CurrentStatus" });
        migrationBuilder.CreateIndex("IX_Notices_Recipient_Created", "Notices",
            new[] { "RecipientId", "CreatedAt" });
        migrationBuilder.CreateIndex("IX_Notifications_Created", "Notifications", "CreatedAt");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("IX_Notifications_Created", "Notifications");
        migrationBuilder.DropIndex("IX_Notices_Recipient_Created", "Notices");
        migrationBuilder.DropIndex("IX_Notices_Office_Status", "Notices");
        migrationBuilder.DropIndex("IX_Notices_Status_Created", "Notices");
    }
}
