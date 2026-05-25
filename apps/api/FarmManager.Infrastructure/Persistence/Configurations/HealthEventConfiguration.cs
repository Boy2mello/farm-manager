using FarmManager.Domain.Health;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManager.Infrastructure.Persistence.Configurations;

internal sealed class HealthEventConfiguration : IEntityTypeConfiguration<HealthEvent>
{
    public void Configure(EntityTypeBuilder<HealthEvent> builder)
    {
        builder.ToTable("health_events");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasColumnName("id");
        builder.Property(h => h.OrganisationId).HasColumnName("organisation_id");
        builder.Property(h => h.AnimalId).HasColumnName("animal_id");
        builder.Property(h => h.EventType).HasColumnName("event_type").HasConversion<int>().IsRequired();
        builder.Property(h => h.EventDate).HasColumnName("event_date").IsRequired();
        builder.Property(h => h.ProductId).HasColumnName("product_id");
        builder.Property(h => h.ProductName).HasColumnName("product_name").HasMaxLength(200);
        builder.Property(h => h.DoseAmount).HasColumnName("dose_amount").HasColumnType("numeric(10,3)");
        builder.Property(h => h.DoseUnit).HasColumnName("dose_unit").HasMaxLength(16);
        builder.Property(h => h.Route).HasColumnName("route").HasConversion<int?>();
        builder.Property(h => h.VetUserId).HasColumnName("vet_user_id");
        builder.Property(h => h.BatchNumber).HasColumnName("batch_number").HasMaxLength(64);
        builder.Property(h => h.Expiry).HasColumnName("expiry");
        builder.Property(h => h.WithdrawalUntil).HasColumnName("withdrawal_until");
        builder.Property(h => h.NextDueDate).HasColumnName("next_due_date");
        builder.Property(h => h.Notes).HasColumnName("notes");
        builder.Property(h => h.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(h => h.UpdatedAt).HasColumnName("updated_at");
        builder.Property(h => h.CreatedBy).HasColumnName("created_by").HasMaxLength(100);
        builder.Property(h => h.UpdatedBy).HasColumnName("updated_by").HasMaxLength(100);

        builder.Ignore(h => h.DomainEvents);

        builder.HasIndex(h => new { h.AnimalId, h.EventDate });
        builder.HasIndex(h => new { h.OrganisationId, h.NextDueDate });
    }
}
