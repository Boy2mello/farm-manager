using FarmManager.Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManager.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("audit_log");

        builder.HasKey(a => a.Sequence);
        builder.Property(a => a.Sequence).HasColumnName("sequence").ValueGeneratedOnAdd();
        builder.Property(a => a.Id).HasColumnName("id").IsRequired();
        builder.Property(a => a.OrganisationId).HasColumnName("organisation_id");
        builder.Property(a => a.UserId).HasColumnName("user_id");
        builder.Property(a => a.UserName).HasColumnName("user_name").HasMaxLength(120);
        builder.Property(a => a.Action).HasColumnName("action").HasMaxLength(80).IsRequired();
        builder.Property(a => a.EntityType).HasColumnName("entity_type").HasMaxLength(80).IsRequired();
        builder.Property(a => a.EntityId).HasColumnName("entity_id").HasMaxLength(64).IsRequired();
        builder.Property(a => a.BeforeJson).HasColumnName("before").HasColumnType("jsonb");
        builder.Property(a => a.AfterJson).HasColumnName("after").HasColumnType("jsonb").IsRequired();
        builder.Property(a => a.OccurredAt).HasColumnName("occurred_at").IsRequired();
        builder.Property(a => a.PreviousHash).HasColumnName("previous_hash").HasMaxLength(64).IsRequired();
        builder.Property(a => a.EntryHash).HasColumnName("entry_hash").HasMaxLength(64).IsRequired();

        builder.HasIndex(a => new { a.OrganisationId, a.OccurredAt });
        builder.HasIndex(a => new { a.EntityType, a.EntityId });
        builder.HasIndex(a => a.Id).IsUnique();
    }
}
