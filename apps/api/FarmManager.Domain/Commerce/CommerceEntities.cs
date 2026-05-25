using FarmManager.Domain.Common;

namespace FarmManager.Domain.Commerce;

public sealed class SaleEvent : Entity<Guid>
{
    public Guid OrganisationId { get; private set; }
    public Guid AnimalId { get; private set; }
    public DateOnly SaleDate { get; private set; }
    public string Buyer { get; private set; } = default!;
    public decimal? WeightKg { get; private set; }
    public decimal PriceTotal { get; private set; }
    public decimal? PricePerKg { get; private set; }
    public decimal Commission { get; private set; }
    public decimal Transport { get; private set; }
    public string? PaperworkReference { get; private set; }
    public string? Notes { get; private set; }

    private SaleEvent() { }

    public static SaleEvent Create(
        Guid organisationId,
        Guid animalId,
        DateOnly saleDate,
        string buyer,
        decimal priceTotal,
        decimal? pricePerKg,
        decimal? weightKg,
        decimal commission,
        decimal transport,
        string? paperworkReference,
        string? notes,
        string? createdBy)
    {
        if (string.IsNullOrWhiteSpace(buyer)) throw new ArgumentException("Buyer is required.", nameof(buyer));
        if (priceTotal <= 0) throw new ArgumentException("Price must be positive.", nameof(priceTotal));

        return new SaleEvent
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisationId,
            AnimalId = animalId,
            SaleDate = saleDate,
            Buyer = buyer.Trim(),
            PriceTotal = priceTotal,
            PricePerKg = pricePerKg,
            WeightKg = weightKg,
            Commission = commission,
            Transport = transport,
            PaperworkReference = paperworkReference,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
        };
    }
}

public sealed class PurchaseEvent : Entity<Guid>
{
    public Guid OrganisationId { get; private set; }
    public Guid AnimalId { get; private set; }
    public DateOnly PurchaseDate { get; private set; }
    public string Seller { get; private set; } = default!;
    public decimal PriceTotal { get; private set; }
    public string? Notes { get; private set; }

    private PurchaseEvent() { }

    public static PurchaseEvent Create(
        Guid organisationId,
        Guid animalId,
        DateOnly purchaseDate,
        string seller,
        decimal priceTotal,
        string? notes,
        string? createdBy)
    {
        if (string.IsNullOrWhiteSpace(seller)) throw new ArgumentException("Seller is required.", nameof(seller));

        return new PurchaseEvent
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisationId,
            AnimalId = animalId,
            PurchaseDate = purchaseDate,
            Seller = seller.Trim(),
            PriceTotal = priceTotal,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
        };
    }
}

public enum DeathCause
{
    Disease = 1,
    Predator = 2,
    Lightning = 3,
    OldAge = 4,
    Calving = 5,
    Accident = 6,
    Theft = 7,
    Unknown = 99,
}

public sealed class DeathEvent : Entity<Guid>
{
    public Guid OrganisationId { get; private set; }
    public Guid AnimalId { get; private set; }
    public DateOnly DeathDate { get; private set; }
    public DeathCause Cause { get; private set; }
    public string? SuspectedDisease { get; private set; }
    public string? PostMortemFindings { get; private set; }
    public bool InsuranceClaimable { get; private set; }
    public string? Notes { get; private set; }

    private DeathEvent() { }

    public static DeathEvent Create(
        Guid organisationId,
        Guid animalId,
        DateOnly deathDate,
        DeathCause cause,
        string? suspectedDisease,
        string? postMortemFindings,
        bool insuranceClaimable,
        string? notes,
        string? createdBy)
    {
        return new DeathEvent
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisationId,
            AnimalId = animalId,
            DeathDate = deathDate,
            Cause = cause,
            SuspectedDisease = suspectedDisease,
            PostMortemFindings = postMortemFindings,
            InsuranceClaimable = insuranceClaimable,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
        };
    }
}

public sealed class TransferEvent : Entity<Guid>
{
    public Guid OrganisationId { get; private set; }
    public Guid AnimalId { get; private set; }
    public Guid FromFarmId { get; private set; }
    public Guid ToFarmId { get; private set; }
    public DateOnly TransferDate { get; private set; }
    public bool Confirmed { get; private set; }
    public string? Notes { get; private set; }

    private TransferEvent() { }

    public static TransferEvent Create(
        Guid organisationId,
        Guid animalId,
        Guid fromFarmId,
        Guid toFarmId,
        DateOnly transferDate,
        string? notes,
        string? createdBy)
    {
        return new TransferEvent
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisationId,
            AnimalId = animalId,
            FromFarmId = fromFarmId,
            ToFarmId = toFarmId,
            TransferDate = transferDate,
            Confirmed = false,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
        };
    }

    public void Confirm() { Confirmed = true; UpdatedAt = DateTimeOffset.UtcNow; }
}
