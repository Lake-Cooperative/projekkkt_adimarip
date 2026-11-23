using Adimarip.Domain.Broadcasts;
using Adimarip.Domain.Directory;
using Adimarip.Domain.Notices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Adimarip.Infrastructure.Persistence.Configurations;

internal sealed class NoticeConfiguration : IEntityTypeConfiguration<Notice>
{
    public void Configure(EntityTypeBuilder<Notice> e)
    {
        e.ToTable("Notices", t =>
        {
            t.HasCheckConstraint("CK_Notices_Deadline", "\"ProcessingDeadline\" >= \"CreatedAt\"");
            t.HasCheckConstraint("CK_Notices_Reason_NotEmpty", "length(trim(\"Reason\")) > 0");
        });
        e.HasKey(x => x.Id);
        e.Property(x => x.Number).HasMaxLength(100).IsRequired();
        e.HasIndex(x => x.Number).IsUnique();
        e.Property(x => x.AuthorUserId).HasMaxLength(100).IsRequired();
        e.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        e.Property(x => x.Comment).HasMaxLength(4000);
        e.Property(x => x.CurrentStatus).HasConversion<string>().HasMaxLength(50).IsRequired();

        // Оптимистическая блокировка по системному столбцу PostgreSQL xmin:
        // параллельное изменение одной записи даёт конфликт, а не «потерянное обновление».
        e.Property<uint>("Version").HasColumnName("xmin").HasColumnType("xid").IsRowVersion();

        e.HasOne(x => x.Recipient).WithMany().HasForeignKey(x => x.RecipientId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.Office).WithMany().HasForeignKey(x => x.OfficeId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.Broadcast).WithMany().HasForeignKey(x => x.BroadcastId).OnDelete(DeleteBehavior.Restrict);

        e.HasMany(x => x.StatusHistory).WithOne().HasForeignKey(x => x.NoticeId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.AuditEvents).WithOne().HasForeignKey(x => x.NoticeId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.Notifications).WithOne().HasForeignKey(x => x.NoticeId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.Appeals).WithOne().HasForeignKey(x => x.NoticeId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.Documents).WithOne().HasForeignKey(x => x.NoticeId).OnDelete(DeleteBehavior.Cascade);

        e.Ignore(x => x.AllowedTransitions);
    }
}

internal sealed class NoticeStatusHistoryConfiguration : IEntityTypeConfiguration<NoticeStatusHistory>
{
    public void Configure(EntityTypeBuilder<NoticeStatusHistory> e)
    {
        e.ToTable("NoticeStatusHistories");
        e.HasKey(x => x.Id);
        e.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        e.Property(x => x.Comment).HasMaxLength(4000);
        e.Property(x => x.ChangedByUserId).HasMaxLength(100).IsRequired();
        e.HasIndex(x => new { x.NoticeId, x.ChangedAt });
    }
}

internal sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> e)
    {
        e.ToTable("AuditEvents");
        e.HasKey(x => x.Id);
        e.Property(x => x.Action).HasMaxLength(100).IsRequired();
        e.Property(x => x.UserId).HasMaxLength(100).IsRequired();
        e.Property(x => x.ObjectType).HasMaxLength(100);
        e.Property(x => x.ObjectId).HasMaxLength(100);
        e.Property(x => x.Details).HasMaxLength(4000);
        e.HasIndex(x => new { x.NoticeId, x.OccurredAt });
    }
}

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> e)
    {
        e.ToTable("Notifications");
        e.HasKey(x => x.Id);
        e.Property(x => x.Channel).HasConversion<string>().HasMaxLength(50).IsRequired();
        e.Property(x => x.RecipientAddress).HasMaxLength(Notification.MaxAddressLength).IsRequired();
        e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        e.Property(x => x.LastError).HasMaxLength(Notification.MaxErrorLength);
        e.HasMany(x => x.DeliveryAttempts).WithOne().HasForeignKey(x => x.NotificationId).OnDelete(DeleteBehavior.Cascade);
        e.HasIndex(x => new { x.NoticeId, x.Status });
        e.Ignore(x => x.IsPending);
    }
}

internal sealed class DeliveryAttemptConfiguration : IEntityTypeConfiguration<DeliveryAttempt>
{
    public void Configure(EntityTypeBuilder<DeliveryAttempt> e)
    {
        e.ToTable("DeliveryAttempts");
        e.HasKey(x => x.Id);
        e.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        e.HasIndex(x => new { x.NotificationId, x.AttemptedAt });
    }
}

internal sealed class AppealConfiguration : IEntityTypeConfiguration<Appeal>
{
    public void Configure(EntityTypeBuilder<Appeal> e)
    {
        e.ToTable("Appeals");
        e.HasKey(x => x.Id);
        e.Property(x => x.Type).HasMaxLength(100).IsRequired();
        e.Property(x => x.Text).HasMaxLength(4000).IsRequired();
        e.Property(x => x.Status).HasMaxLength(50).IsRequired();
    }
}

internal sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> e)
    {
        e.ToTable("Documents");
        e.HasKey(x => x.Id);
        e.Property(x => x.Name).HasMaxLength(255).IsRequired();
        e.Property(x => x.MimeType).HasMaxLength(150).IsRequired();
        e.Property(x => x.StorageUri).HasMaxLength(2048).IsRequired();
    }
}
