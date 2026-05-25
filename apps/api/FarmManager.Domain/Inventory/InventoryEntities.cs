using FarmManager.Domain.Common;

namespace FarmManager.Domain.Inventory;

public enum InventoryCategory
{
    Feed = 1,
    Supplement = 2,
    Medicine = 3,
    Vaccine = 4,
    Dip = 5,
    Equipment = 6,
    Other = 99,
}

public enum StockMovementKind
{
    PurchaseIn = 1,
    UsageOut = 2,
    TransferIn = 3,
    TransferOut = 4,
    WriteOff = 5,
    Adjustment = 6,
}

public sealed class InventoryItem : Entity<Guid>
{
    public Guid OrganisationId { get; private set; }
    public string Sku { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public InventoryCategory Category { get; private set; }
    public string Unit { get; private set; } = "unit";
    public decimal OnHandQuantity { get; private set; }
    public decimal? ReorderPoint { get; private set; }
    public decimal? CostPerUnit { get; private set; }
    public bool ColdChain { get; private set; }

    private InventoryItem() { }

    public static InventoryItem Create(
        Guid organisationId,
        string sku,
        string name,
        InventoryCategory category,
        string unit,
        decimal? reorderPoint,
        decimal? costPerUnit,
        bool coldChain)
    {
        if (string.IsNullOrWhiteSpace(sku)) throw new ArgumentException("SKU is required.", nameof(sku));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));

        return new InventoryItem
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisationId,
            Sku = sku.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Category = category,
            Unit = unit,
            ReorderPoint = reorderPoint,
            CostPerUnit = costPerUnit,
            ColdChain = coldChain,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void Apply(decimal delta)
    {
        OnHandQuantity += delta;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public sealed class StockMovement : Entity<Guid>
{
    public Guid OrganisationId { get; private set; }
    public Guid ItemId { get; private set; }
    public StockMovementKind Kind { get; private set; }
    public decimal Quantity { get; private set; }
    public DateOnly MovementDate { get; private set; }
    public string? BatchNumber { get; private set; }
    public DateOnly? Expiry { get; private set; }
    public Guid? RelatedEventId { get; private set; }
    public string? Notes { get; private set; }

    private StockMovement() { }

    public static StockMovement Create(
        Guid organisationId,
        Guid itemId,
        StockMovementKind kind,
        decimal quantity,
        DateOnly movementDate,
        string? batchNumber,
        DateOnly? expiry,
        Guid? relatedEventId,
        string? notes,
        string? createdBy)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive.", nameof(quantity));

        return new StockMovement
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisationId,
            ItemId = itemId,
            Kind = kind,
            Quantity = quantity,
            MovementDate = movementDate,
            BatchNumber = batchNumber,
            Expiry = expiry,
            RelatedEventId = relatedEventId,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
        };
    }

    public decimal SignedDelta() => Kind switch
    {
        StockMovementKind.PurchaseIn or StockMovementKind.TransferIn or StockMovementKind.Adjustment => +Quantity,
        StockMovementKind.UsageOut or StockMovementKind.TransferOut or StockMovementKind.WriteOff => -Quantity,
        _ => 0,
    };
}
