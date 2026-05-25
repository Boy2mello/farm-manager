using FarmManager.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManager.Infrastructure.Persistence.Configurations;

internal sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("inventory_items");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id");
        builder.Property(i => i.OrganisationId).HasColumnName("organisation_id");
        builder.Property(i => i.Sku).HasColumnName("sku").HasMaxLength(64).IsRequired();
        builder.Property(i => i.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(i => i.Category).HasColumnName("category").HasConversion<int>().IsRequired();
        builder.Property(i => i.Unit).HasColumnName("unit").HasMaxLength(16).IsRequired();
        builder.Property(i => i.OnHandQuantity).HasColumnName("on_hand").HasColumnType("numeric(14,4)");
        builder.Property(i => i.ReorderPoint).HasColumnName("reorder_point").HasColumnType("numeric(14,4)");
        builder.Property(i => i.CostPerUnit).HasColumnName("cost_per_unit").HasColumnType("numeric(14,4)");
        builder.Property(i => i.ColdChain).HasColumnName("cold_chain").IsRequired();

        builder.Property(i => i.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(i => i.UpdatedAt).HasColumnName("updated_at");
        builder.Property(i => i.CreatedBy).HasColumnName("created_by").HasMaxLength(100);
        builder.Property(i => i.UpdatedBy).HasColumnName("updated_by").HasMaxLength(100);

        builder.Ignore(i => i.DomainEvents);
        builder.HasIndex(i => new { i.OrganisationId, i.Sku }).IsUnique();
    }
}

internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movements");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");
        builder.Property(m => m.OrganisationId).HasColumnName("organisation_id");
        builder.Property(m => m.ItemId).HasColumnName("item_id");
        builder.Property(m => m.Kind).HasColumnName("kind").HasConversion<int>().IsRequired();
        builder.Property(m => m.Quantity).HasColumnName("quantity").HasColumnType("numeric(14,4)").IsRequired();
        builder.Property(m => m.MovementDate).HasColumnName("movement_date").IsRequired();
        builder.Property(m => m.BatchNumber).HasColumnName("batch_number").HasMaxLength(64);
        builder.Property(m => m.Expiry).HasColumnName("expiry");
        builder.Property(m => m.RelatedEventId).HasColumnName("related_event_id");
        builder.Property(m => m.Notes).HasColumnName("notes");
        builder.Property(m => m.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(m => m.UpdatedAt).HasColumnName("updated_at");
        builder.Property(m => m.CreatedBy).HasColumnName("created_by").HasMaxLength(100);
        builder.Property(m => m.UpdatedBy).HasColumnName("updated_by").HasMaxLength(100);

        builder.Ignore(m => m.DomainEvents);
        builder.HasIndex(m => new { m.ItemId, m.MovementDate });
        builder.HasIndex(m => m.Expiry);
    }
}
