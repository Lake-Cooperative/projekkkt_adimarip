using Adimarip.Domain.Broadcasts;
using Adimarip.Domain.Directory;
using Adimarip.Domain.Notices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Adimarip.Infrastructure.Persistence.Configurations;

internal sealed class BroadcastConfiguration : IEntityTypeConfiguration<Broadcast>
{
    public void Configure(EntityTypeBuilder<Broadcast> e)
    {
        e.ToTable("Broadcasts");
        e.HasKey(x => x.Id);
        e.Property(x => x.Code).HasMaxLength(40).IsRequired();
        e.HasIndex(x => x.Code).IsUnique();
        e.Property(x => x.Title).HasMaxLength(Broadcast.MaxTitleLength).IsRequired();
        e.Property(x => x.Message).HasMaxLength(Broadcast.MaxMessageLength).IsRequired();
        e.Property(x => x.Severity).HasConversion<string>().HasMaxLength(20).IsRequired();
        e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        e.Property(x => x.Channels)
            .HasConversion(ChannelListConversion.Converter, ChannelListConversion.Comparer)
            .HasMaxLength(100).IsRequired();
        e.Property(x => x.AudienceDescription).HasMaxLength(300).IsRequired();
        e.Property(x => x.CreatedByUserId).HasMaxLength(100).IsRequired();
        e.HasOne<Office>().WithMany().HasForeignKey(x => x.OfficeId).OnDelete(DeleteBehavior.Restrict);
        e.HasIndex(x => x.Status);
        e.Ignore(x => x.IsFinished);
        e.Ignore(x => x.ProcessingDays);
    }
}

/// <summary>Список каналов хранится строкой «Email,Sms».</summary>
internal static class ChannelListConversion
{
    public static readonly ValueConverter<IReadOnlyList<DeliveryChannel>, string> Converter = new(
        channels => string.Join(',', channels),
        text => Parse(text));

    public static readonly ValueComparer<IReadOnlyList<DeliveryChannel>> Comparer = new(
        (left, right) => (left ?? Array.Empty<DeliveryChannel>()).SequenceEqual(right ?? Array.Empty<DeliveryChannel>()),
        channels => channels.Aggregate(0, (hash, channel) => HashCode.Combine(hash, channel)),
        channels => channels.ToArray());

    private static IReadOnlyList<DeliveryChannel> Parse(string text) =>
        text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Enum.Parse<DeliveryChannel>)
            .ToArray();
}
