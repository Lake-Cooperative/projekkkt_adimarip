using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Adimarip.Data.Migrations;

[DbContext(typeof(AdimaripDbContext))]
[Migration("202610160003_DomainChecks")]
public sealed class DomainChecks : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddCheckConstraint("CK_Notices_Status_Allowed", "Notices",
            "\"CurrentStatus\" IN ('Created','Sent','Delivered','Read','Closed','Cancelled','Failed')");
        migrationBuilder.AddCheckConstraint("CK_Notifications_Channel_Allowed", "Notifications",
            "\"Channel\" IN ('Email','Sms','Portal','Max')");
        migrationBuilder.AddCheckConstraint("CK_Appeals_Text_NotBlank", "Appeals",
            "length(trim(\"Text\")) > 0");
        migrationBuilder.AddCheckConstraint("CK_DeliveryAttempts_Status_Allowed", "DeliveryAttempts",
            "\"Status\" IN ('Delivered','Failed','Queued')");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint("CK_DeliveryAttempts_Status_Allowed", "DeliveryAttempts");
        migrationBuilder.DropCheckConstraint("CK_Appeals_Text_NotBlank", "Appeals");
        migrationBuilder.DropCheckConstraint("CK_Notifications_Channel_Allowed", "Notifications");
        migrationBuilder.DropCheckConstraint("CK_Notices_Status_Allowed", "Notices");
    }
}
