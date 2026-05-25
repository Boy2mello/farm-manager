using FarmManager.Domain.Flagging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManager.Infrastructure.Persistence.Configurations;

internal sealed class FlagConfiguration : IEntityTypeConfiguration<Flag>
{
    public void Configure(EntityTypeBuilder<Flag> builder)
    {
        builder.ToTable("flags");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id");
        builder.Property(f => f.OrganisationId).HasColumnName("organisation_id");
        builder.Property(f => f.AnimalId).HasColumnName("animal_id");
        builder.Property(f => f.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
        builder.Property(f => f.Severity).HasColumnName("severity").HasConversion<int>().IsRequired();
        builder.Property(f => f.AssignedAt).HasColumnName("assigned_at").IsRequired();
        builder.Property(f => f.ResolvedAt).HasColumnName("resolved_at");
        builder.Property(f => f.Reason).HasColumnName("reason").IsRequired();
        builder.Property(f => f.MetricsJson).HasColumnName("metrics").HasColumnType("jsonb");
        builder.Property(f => f.SourceEventId).HasColumnName("source_event_id");

        builder.HasIndex(f => new { f.AnimalId, f.Code }).IsUnique()
            .HasFilter("resolved_at IS NULL");
        builder.HasIndex(f => new { f.OrganisationId, f.Severity });
    }
}

internal sealed class TierAssignmentConfiguration : IEntityTypeConfiguration<TierAssignment>
{
    public void Configure(EntityTypeBuilder<TierAssignment> builder)
    {
        builder.ToTable("tier_assignments");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.OrganisationId).HasColumnName("organisation_id");
        builder.Property(t => t.AnimalId).HasColumnName("animal_id");
        builder.Property(t => t.PreviousTier).HasColumnName("previous_tier").HasConversion<int>().IsRequired();
        builder.Property(t => t.Tier).HasColumnName("tier").HasConversion<int>().IsRequired();
        builder.Property(t => t.AssignedAt).HasColumnName("assigned_at").IsRequired();
        builder.Property(t => t.AssignedBy).HasColumnName("assigned_by").HasMaxLength(100).IsRequired();
        builder.Property(t => t.Reason).HasColumnName("reason").IsRequired();
        builder.Property(t => t.MetricsJson).HasColumnName("metrics").HasColumnType("jsonb");

        builder.HasIndex(t => t.AnimalId);
        builder.HasIndex(t => t.AssignedAt);
    }
}
