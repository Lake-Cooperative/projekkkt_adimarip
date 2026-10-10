using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Adimarip.Data.Migrations;

public partial class InitialDomainSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Offices",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Offices", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Recipients",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ExternalIdentifier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                FullName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_Recipients", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Addresses",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                RecipientId = table.Column<int>(type: "integer", nullable: false),
                PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                Region = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                City = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                Street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                House = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                Apartment = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Addresses", x => x.Id);
                table.ForeignKey("FK_Addresses_Recipients_RecipientId", x => x.RecipientId, "Recipients", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Notices",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                RecipientId = table.Column<int>(type: "integer", nullable: false),
                OfficeId = table.Column<int>(type: "integer", nullable: false),
                AuthorUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                CreatedAt = table.Column<DateOnly>(type: "date", nullable: false),
                ProcessingDeadline = table.Column<DateOnly>(type: "date", nullable: false),
                Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                Comment = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                CurrentStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Notices", x => x.Id);
                table.CheckConstraint("CK_Notices_Deadline", "\\"ProcessingDeadline\\" >= \\"CreatedAt\\"");
                table.CheckConstraint("CK_Notices_Reason_NotEmpty", "length(trim(\\"Reason\\")) > 0");
                table.ForeignKey("FK_Notices_Offices_OfficeId", x => x.OfficeId, "Offices", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_Notices_Recipients_RecipientId", x => x.RecipientId, "Recipients", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Appeals",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                NoticeId = table.Column<int>(type: "integer", nullable: false),
                Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Appeals", x => x.Id);
                table.ForeignKey("FK_Appeals_Notices_NoticeId", x => x.NoticeId, "Notices", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AuditEvents",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                NoticeId = table.Column<int>(type: "integer", nullable: false),
                Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ObjectType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                ObjectId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                Details = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditEvents", x => x.Id);
                table.ForeignKey("FK_AuditEvents_Notices_NoticeId", x => x.NoticeId, "Notices", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Documents",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                NoticeId = table.Column<int>(type: "integer", nullable: false),
                Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                MimeType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                StorageUri = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                AddedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Documents", x => x.Id);
                table.ForeignKey("FK_Documents_Notices_NoticeId", x => x.NoticeId, "Notices", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Notifications",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                NoticeId = table.Column<int>(type: "integer", nullable: false),
                Channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                RecipientAddress = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Notifications", x => x.Id);
                table.ForeignKey("FK_Notifications_Notices_NoticeId", x => x.NoticeId, "Notices", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "NoticeStatusHistories",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                NoticeId = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Comment = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ChangedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NoticeStatusHistories", x => x.Id);
                table.ForeignKey("FK_NoticeStatusHistories_Notices_NoticeId", x => x.NoticeId, "Notices", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DeliveryAttempts",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                NotificationId = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                AttemptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DeliveryAttempts", x => x.Id);
                table.ForeignKey("FK_DeliveryAttempts_Notifications_NotificationId", x => x.NotificationId, "Notifications", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_Offices_Name", "Offices", "Name", unique: true);
        migrationBuilder.CreateIndex("IX_Recipients_ExternalIdentifier", "Recipients", "ExternalIdentifier", unique: true);
        migrationBuilder.CreateIndex("IX_Addresses_RecipientId", "Addresses", "RecipientId", unique: true);
        migrationBuilder.CreateIndex("IX_Notices_Number", "Notices", "Number", unique: true);
        migrationBuilder.CreateIndex("IX_Notices_OfficeId", "Notices", "OfficeId");
        migrationBuilder.CreateIndex("IX_Notices_RecipientId", "Notices", "RecipientId");
        migrationBuilder.CreateIndex("IX_Appeals_NoticeId", "Appeals", "NoticeId");
        migrationBuilder.CreateIndex("IX_AuditEvents_NoticeId_OccurredAt", "AuditEvents", new[] { "NoticeId", "OccurredAt" });
        migrationBuilder.CreateIndex("IX_Documents_NoticeId", "Documents", "NoticeId");
        migrationBuilder.CreateIndex("IX_Notifications_NoticeId", "Notifications", "NoticeId");
        migrationBuilder.CreateIndex("IX_NoticeStatusHistories_NoticeId_ChangedAt", "NoticeStatusHistories", new[] { "NoticeId", "ChangedAt" });
        migrationBuilder.CreateIndex("IX_DeliveryAttempts_NotificationId_AttemptedAt", "DeliveryAttempts", new[] { "NotificationId", "AttemptedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Addresses");
        migrationBuilder.DropTable(name: "Appeals");
        migrationBuilder.DropTable(name: "AuditEvents");
        migrationBuilder.DropTable(name: "Documents");
        migrationBuilder.DropTable(name: "NoticeStatusHistories");
        migrationBuilder.DropTable(name: "DeliveryAttempts");
        migrationBuilder.DropTable(name: "Notifications");
        migrationBuilder.DropTable(name: "Notices");
        migrationBuilder.DropTable(name: "Recipients");
        migrationBuilder.DropTable(name: "Offices");
    }
}
