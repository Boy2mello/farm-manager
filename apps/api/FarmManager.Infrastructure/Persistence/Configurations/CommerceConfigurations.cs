using FarmManager.Domain.Commerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManager.Infrastructure.Persistence.Configurations;

internal sealed class SaleEventConfiguration : IEntityTypeConfiguration<SaleEvent>
{
    public void Configure(EntityTypeBuilder<SaleEvent> builder)
    {
        builder.ToTable("sale_events");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.OrganisationId).HasColumnName("organisation_id");
        builder.Property(s => s.AnimalId).HasColumnName("animal_id");
        builder.Property(s => s.SaleDate).HasColumnName("sale_date").IsRequired();
        builder.Property(s => s.Buyer).HasColumnName("buyer").HasMaxLength(200).IsRequired();
        builder.Property(s => s.WeightKg).HasColumnName("weight_kg").HasColumnType("numeric(8,2)");
        builder.Property(s => s.PriceTotal).HasColumnName("price_total").HasColumnType("numeric(14,2)").IsRequired();
        builder.Property(s => s.PricePerKg).HasColumnName("price_per_kg").HasColumnType("numeric(10,2)");
        builder.Property(s => s.Commission).HasColumnName("commission").HasColumnType("numeric(14,2)");
        builder.Property(s => s.Transport).HasColumnName("transport").HasColumnType("numeric(14,2)");
        builder.Property(s => s.PaperworkReference).HasColumnName("paperwork_reference").HasMaxLength(120);
        builder.Property(s => s.Notes).HasColumnName("notes");
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at");
        builder.Property(s => s.CreatedBy).HasColumnName("created_by").HasMaxLength(100);
        builder.Property(s => s.UpdatedBy).HasColumnName("updated_by").HasMaxLength(100);

        builder.Ignore(s => s.DomainEvents);
        // The "first sale wins" conflict rule (spec §7.3): unique by animal id, alive forever.
        builder.HasIndex(s => s.AnimalId).IsUnique();
    }
}

internal sealed class PurchaseEventConfiguration : IEntityTypeConfiguration<PurchaseEvent>
{
    public void Configure(EntityTypeBuilder<PurchaseEvent> builder)
    {
        builder.ToTable("purchase_events");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.OrganisationId).HasColumnName("organisation_id");
        builder.Property(p => p.AnimalId).HasColumnName("animal_id");
        builder.Property(p => p.PurchaseDate).HasColumnName("purchase_date").IsRequired();
        builder.Property(p => p.Seller).HasColumnName("seller").HasMaxLength(200).IsRequired();
        builder.Property(p => p.PriceTotal).HasColumnName("price_total").HasColumnType("numeric(14,2)").IsRequired();
        builder.Property(p => p.Notes).HasColumnName("notes");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.Property(p => p.CreatedBy).HasColumnName("created_by").HasMaxLength(100);
        builder.Property(p => p.UpdatedBy).HasColumnName("updated_by").HasMaxLength(100);

        builder.Ignore(p => p.DomainEvents);
    }
}

internal sealed class DeathEventConfiguration : IEntityTypeConfiguration<DeathEvent>
{
    public void Configure(EntityTypeBuilder<DeathEvent> builder)
    {
        builder.ToTable("death_events");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id");
        builder.Property(d => d.OrganisationId).HasColumnName("organisation_id");
        builder.Property(d => d.AnimalId).HasColumnName("animal_id");
        builder.Property(d => d.DeathDate).HasColumnName("death_date").IsRequired();
        builder.Property(d => d.Cause).HasColumnName("cause").HasConversion<int>().IsRequired();
        builder.Property(d => d.SuspectedDisease).HasColumnName("suspected_disease").HasMaxLength(200);
        builder.Property(d => d.PostMortemFindings).HasColumnName("post_mortem_findings");
        builder.Property(d => d.InsuranceClaimable).HasColumnName("insurance_claimable").IsRequired();
        builder.Property(d => d.Notes).HasColumnName("notes");
        builder.Property(d => d.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at");
        builder.Property(d => d.CreatedBy).HasColumnName("created_by").HasMaxLength(100);
        builder.Property(d => d.UpdatedBy).HasColumnName("updated_by").HasMaxLength(100);

        builder.Ignore(d => d.DomainEvents);
        builder.HasIndex(d => d.AnimalId).IsUnique();
    }
}

internal sealed class TransferEventConfiguration : IEntityTypeConfiguration<TransferEvent>
{
    public void Configure(EntityTypeBuilder<TransferEvent> builder)
    {
        builder.ToTable("transfer_events");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.OrganisationId).HasColumnName("organisation_id");
        builder.Property(t => t.AnimalId).HasColumnName("animal_id");
        builder.Property(t => t.FromFarmId).HasColumnName("from_farm_id");
        builder.Property(t => t.ToFarmId).HasColumnName("to_farm_id");
        builder.Property(t => t.TransferDate).HasColumnName("transfer_date").IsRequired();
        builder.Property(t => t.Confirmed).HasColumnName("confirmed").IsRequired();
        builder.Property(t => t.Notes).HasColumnName("notes");
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");
        builder.Property(t => t.CreatedBy).HasColumnName("created_by").HasMaxLength(100);
        builder.Property(t => t.UpdatedBy).HasColumnName("updated_by").HasMaxLength(100);

        builder.Ignore(t => t.DomainEvents);
    }
}
