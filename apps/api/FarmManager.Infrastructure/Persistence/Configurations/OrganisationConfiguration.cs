using FarmManager.Domain.Organisations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManager.Infrastructure.Persistence.Configurations;

internal sealed class OrganisationConfiguration : IEntityTypeConfiguration<Organisation>
{
    public void Configure(EntityTypeBuilder<Organisation> builder)
    {
        builder.ToTable("organisations");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("id");

        builder.Property(o => o.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(o => o.TimeZone).HasColumnName("time_zone").HasMaxLength(64).IsRequired();
        builder.Property(o => o.CalfPrefix).HasColumnName("calf_prefix").HasMaxLength(8).IsRequired();
        builder.Property(o => o.PurchasedPrefix).HasColumnName("purchased_prefix").HasMaxLength(8).IsRequired();
        builder.Property(o => o.LegacyPrefix).HasColumnName("legacy_prefix").HasMaxLength(8).IsRequired();
        builder.Property(o => o.SequenceWidth).HasColumnName("sequence_width").IsRequired();

        builder.Property(o => o.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(o => o.UpdatedAt).HasColumnName("updated_at");
        builder.Property(o => o.CreatedBy).HasColumnName("created_by").HasMaxLength(100);
        builder.Property(o => o.UpdatedBy).HasColumnName("updated_by").HasMaxLength(100);

        builder.Ignore(o => o.DomainEvents);

        builder.HasIndex(o => o.Name).IsUnique();
    }
}
