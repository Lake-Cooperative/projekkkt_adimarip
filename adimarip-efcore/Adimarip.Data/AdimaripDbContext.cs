using Adimarip.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Adimarip.Data;

public sealed class AdimaripDbContext(DbContextOptions<AdimaripDbContext> options) : DbContext(options)
{
    public DbSet<Recipient> Recipients => Set<Recipient>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Office> Offices => Set<Office>();
    public DbSet<Notice> Notices => Set<Notice>();
    public DbSet<NoticeStatusHistory> NoticeStatusHistories => Set<NoticeStatusHistory>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<DeliveryAttempt> DeliveryAttempts => Set<DeliveryAttempt>();
    public DbSet<Appeal> Appeals => Set<Appeal>();
    public DbSet<Document> Documents => Set<Document>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Recipient>(e =>
        {
            e.ToTable("Recipients");
            e.HasKey(x => x.Id);
            e.Property(x => x.ExternalIdentifier).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.ExternalIdentifier).IsUnique();
            e.Property(x => x.FullName).HasMaxLength(250).IsRequired();
            e.Property(x => x.Email).HasMaxLength(320);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.HasOne(x => x.Address).WithOne(x => x.Recipient)
                .HasForeignKey<Address>(x => x.RecipientId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Address>(e =>
        {
            e.ToTable("Addresses");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.RecipientId).IsUnique();
            e.Property(x => x.PostalCode).HasMaxLength(20);
            e.Property(x => x.Region).HasMaxLength(150);
            e.Property(x => x.City).HasMaxLength(150);
            e.Property(x => x.Street).HasMaxLength(200);
            e.Property(x => x.House).HasMaxLength(30);
            e.Property(x => x.Apartment).HasMaxLength(30);
        });

        modelBuilder.Entity<Office>(e =>
        {
            e.ToTable("Offices");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Notice>(e =>
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
            e.Property(x => x.CurrentStatus).HasMaxLength(50).IsRequired();
            e.HasOne(x => x.Recipient).WithMany(x => x.Notices)
                .HasForeignKey(x => x.RecipientId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Office).WithMany(x => x.Notices)
                .HasForeignKey(x => x.OfficeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<NoticeStatusHistory>(e =>
        {
            e.ToTable("NoticeStatusHistories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasMaxLength(50).IsRequired();
            e.Property(x => x.Comment).HasMaxLength(4000);
            e.Property(x => x.ChangedByUserId).HasMaxLength(100).IsRequired();
            e.HasOne(x => x.Notice).WithMany(x => x.StatusHistory)
                .HasForeignKey(x => x.NoticeId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.NoticeId, x.ChangedAt });
        });

        modelBuilder.Entity<AuditEvent>(e =>
        {
            e.ToTable("AuditEvents");
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(100).IsRequired();
            e.Property(x => x.UserId).HasMaxLength(100).IsRequired();
            e.Property(x => x.ObjectType).HasMaxLength(100);
            e.Property(x => x.ObjectId).HasMaxLength(100);
            e.Property(x => x.Details).HasMaxLength(4000);
            e.HasOne(x => x.Notice).WithMany(x => x.AuditEvents)
                .HasForeignKey(x => x.NoticeId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.NoticeId, x.OccurredAt });
        });

        modelBuilder.Entity<Notification>(e =>
        {
            e.ToTable("Notifications");
            e.HasKey(x => x.Id);
            e.Property(x => x.Channel).HasMaxLength(50).IsRequired();
            e.Property(x => x.RecipientAddress).HasMaxLength(320).IsRequired();
            e.HasOne(x => x.Notice).WithMany(x => x.Notifications)
                .HasForeignKey(x => x.NoticeId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DeliveryAttempt>(e =>
        {
            e.ToTable("DeliveryAttempts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasMaxLength(50).IsRequired();
            e.HasOne(x => x.Notification).WithMany(x => x.DeliveryAttempts)
                .HasForeignKey(x => x.NotificationId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.NotificationId, x.AttemptedAt });
        });

        modelBuilder.Entity<Appeal>(e =>
        {
            e.ToTable("Appeals");
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasMaxLength(100).IsRequired();
            e.Property(x => x.Text).HasMaxLength(4000).IsRequired();
            e.Property(x => x.Status).HasMaxLength(50).IsRequired();
            e.HasOne(x => x.Notice).WithMany(x => x.Appeals)
                .HasForeignKey(x => x.NoticeId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Document>(e =>
        {
            e.ToTable("Documents");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(255).IsRequired();
            e.Property(x => x.MimeType).HasMaxLength(150).IsRequired();
            e.Property(x => x.StorageUri).HasMaxLength(2048).IsRequired();
            e.HasOne(x => x.Notice).WithMany(x => x.Documents)
                .HasForeignKey(x => x.NoticeId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
