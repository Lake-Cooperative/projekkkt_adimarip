using Adimarip.Domain.Access;
using Adimarip.Domain.Directory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Adimarip.Infrastructure.Persistence.Configurations;

internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> e)
    {
        e.ToTable("AppUsers");
        e.HasKey(x => x.Username);
        e.Property(x => x.Username).HasMaxLength(100).ValueGeneratedNever();
        e.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        e.Property(x => x.RoleName).HasColumnName("Role").HasMaxLength(40).IsRequired();
        e.Property(x => x.PasswordHash).IsRequired();
        e.HasOne<Role>().WithMany().HasForeignKey(x => x.RoleName).HasPrincipalKey(r => r.Name)
            .OnDelete(DeleteBehavior.Restrict);
        e.HasOne<Recipient>().WithMany().HasForeignKey(x => x.RecipientId).OnDelete(DeleteBehavior.Restrict);
        e.HasIndex(x => x.RoleName).HasDatabaseName("IX_AppUsers_Role");
    }
}

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> e)
    {
        e.ToTable("Roles");
        e.HasKey(x => x.Name);
        e.Property(x => x.Name).HasMaxLength(40).ValueGeneratedNever();
        e.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
        e.HasMany(x => x.Permissions).WithOne().HasForeignKey(x => x.RoleName).OnDelete(DeleteBehavior.Cascade);
        e.Ignore(x => x.PermissionSet);
        e.Ignore(x => x.RequiresRecipientBinding);
    }
}

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> e)
    {
        e.ToTable("RolePermissions");
        e.HasKey(x => new { x.RoleName, x.Permission });
        e.Property(x => x.RoleName).HasMaxLength(40);
        e.Property(x => x.Permission).HasConversion<string>().HasMaxLength(50);
    }
}
