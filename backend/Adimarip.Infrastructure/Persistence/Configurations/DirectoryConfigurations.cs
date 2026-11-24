using Adimarip.Domain.Directory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Adimarip.Infrastructure.Persistence.Configurations;

internal sealed class OfficeConfiguration : IEntityTypeConfiguration<Office>
{
    public void Configure(EntityTypeBuilder<Office> e)
    {
        e.ToTable("Offices");
        e.HasKey(x => x.Id);
        e.Property(x => x.Name).HasMaxLength(200).IsRequired();
        e.HasIndex(x => x.Name).IsUnique();
    }
}

internal sealed class RecipientConfiguration : IEntityTypeConfiguration<Recipient>
{
    public void Configure(EntityTypeBuilder<Recipient> e)
    {
        e.ToTable("Recipients");
        e.HasKey(x => x.Id);
        e.Property(x => x.ExternalIdentifier).HasMaxLength(100).IsRequired();
        e.HasIndex(x => x.ExternalIdentifier).IsUnique();
        e.Property(x => x.FullName).HasMaxLength(250).IsRequired();
        e.Property(x => x.Email).HasMaxLength(320);
        e.Property(x => x.Phone).HasMaxLength(50);
        e.HasOne<Office>().WithMany().HasForeignKey(x => x.OfficeId).OnDelete(DeleteBehavior.SetNull);
        e.HasOne(x => x.Address).WithOne()
            .HasForeignKey<Address>(x => x.RecipientId).OnDelete(DeleteBehavior.Cascade);
        e.HasOne(x => x.ArchiveRecord).WithOne()
            .HasForeignKey<RecipientArchive>(x => x.RecipientId).OnDelete(DeleteBehavior.Restrict);
        e.Ignore(x => x.IsArchived);
    }
}

internal sealed class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> e)
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
    }
}

internal sealed class RecipientArchiveConfiguration : IEntityTypeConfiguration<RecipientArchive>
{
    public void Configure(EntityTypeBuilder<RecipientArchive> e)
    {
        e.ToTable("RecipientArchives");
        e.HasKey(x => x.RecipientId);
        e.Property(x => x.RecipientId).ValueGeneratedNever();
        e.Property(x => x.ArchivedByUserId).HasMaxLength(100).IsRequired();
        e.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
    }
}
