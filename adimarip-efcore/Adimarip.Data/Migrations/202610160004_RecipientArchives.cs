using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Adimarip.Data.Migrations;

[DbContext(typeof(AdimaripDbContext))]
[Migration("202610160004_RecipientArchives")]
public sealed class RecipientArchives : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "RecipientArchives",
            columns: table => new
            {
                RecipientId = table.Column<int>(type: "integer", nullable: false),
                ArchivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ArchivedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RecipientArchives", x => x.RecipientId);
                table.ForeignKey("FK_RecipientArchives_Recipients", x => x.RecipientId,
                    "Recipients", "Id", onDelete: ReferentialAction.Restrict);
            });
    }
    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "RecipientArchives");
}
