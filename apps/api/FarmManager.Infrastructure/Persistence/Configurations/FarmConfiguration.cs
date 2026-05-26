using FarmManager.Domain.Organisations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManager.Infrastructure.Persistence.Configurations;

internal sealed class FarmConfiguration : IEntityTypeConfiguration<Farm>
{
    public void Configure(EntityTypeBuilder<Farm> builder)
    {
        builder.ToTable("farms");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id");
        builder.Property(f => f.OrganisationId).HasColumnName("organisation_id").IsRequired();
        builder.Property(f => f.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
        builder.Property(f => f.OwnerName).HasColumnName("owner_name").HasMaxLength(120);
        builder.Property(f => f.Notes).HasColumnName("notes");

        builder.Property(f => f.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(f => f.UpdatedAt).HasColumnName("updated_at");
        builder.Property(f => f.CreatedBy).HasColumnName("created_by").HasMaxLength(100);
        builder.Property(f => f.UpdatedBy).HasColumnName("updated_by").HasMaxLength(100);

        builder.Ignore(f => f.DomainEvents);

        builder.HasIndex(f => new { f.OrganisationId, f.Name }).IsUnique();
    }
}
