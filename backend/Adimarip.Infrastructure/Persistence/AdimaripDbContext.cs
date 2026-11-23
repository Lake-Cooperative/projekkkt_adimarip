using Adimarip.Domain.Access;
using Adimarip.Domain.Broadcasts;
using Adimarip.Domain.Directory;
using Adimarip.Domain.Notices;
using Microsoft.EntityFrameworkCore;

namespace Adimarip.Infrastructure.Persistence;

/// <summary>
/// Контекст БД. Отображение каждой сущности вынесено в отдельный класс
/// IEntityTypeConfiguration (папка Configurations), чтобы не было «простыни» в OnModelCreating.
/// </summary>
public sealed class AdimaripDbContext(DbContextOptions<AdimaripDbContext> options) : DbContext(options)
{
    public DbSet<Recipient> Recipients => Set<Recipient>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<RecipientArchive> RecipientArchives => Set<RecipientArchive>();
    public DbSet<Office> Offices => Set<Office>();
    public DbSet<Notice> Notices => Set<Notice>();
    public DbSet<NoticeStatusHistory> NoticeStatusHistories => Set<NoticeStatusHistory>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<DeliveryAttempt> DeliveryAttempts => Set<DeliveryAttempt>();
    public DbSet<Appeal> Appeals => Set<Appeal>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Broadcast> Broadcasts => Set<Broadcast>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AdimaripDbContext).Assembly);
    }
}
