using FarmManager.Domain.Common;

namespace FarmManager.Domain.Health;

public enum HealthEventType
{
    Vaccination = 1,
    Treatment = 2,
    Deworming = 3,
    Dipping = 4,
    PregnancyCheck = 5,
    Observation = 6,
}

public enum AdministrationRoute
{
    Oral = 1,
    Subcutaneous = 2,
    Intramuscular = 3,
    Intravenous = 4,
    Topical = 5,
    Pour = 6,
    Other = 99,
}

public sealed class HealthEvent : Entity<Guid>
{
    public Guid OrganisationId { get; private set; }
    public Guid AnimalId { get; private set; }
    public HealthEventType EventType { get; private set; }
    public DateOnly EventDate { get; private set; }
    public Guid? ProductId { get; private set; }
    public string? ProductName { get; private set; }
    public decimal? DoseAmount { get; private set; }
    public string? DoseUnit { get; private set; }
    public AdministrationRoute? Route { get; private set; }
    public Guid? VetUserId { get; private set; }
    public string? BatchNumber { get; private set; }
    public DateOnly? Expiry { get; private set; }
    public DateOnly? WithdrawalUntil { get; private set; }
    public DateOnly? NextDueDate { get; private set; }
    public string? Notes { get; private set; }

    private HealthEvent() { }

    public static HealthEvent Create(
        Guid organisationId,
        Guid animalId,
        HealthEventType type,
        DateOnly eventDate,
        Guid? productId,
        string? productName,
        decimal? doseAmount,
        string? doseUnit,
        AdministrationRoute? route,
        Guid? vetUserId,
        string? batchNumber,
        DateOnly? expiry,
        DateOnly? withdrawalUntil,
        DateOnly? nextDue,
        string? notes,
        string? createdBy)
    {
        if (eventDate > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ArgumentException("Event date cannot be in the future.", nameof(eventDate));
        }

        return new HealthEvent
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisationId,
            AnimalId = animalId,
            EventType = type,
            EventDate = eventDate,
            ProductId = productId,
            ProductName = productName,
            DoseAmount = doseAmount,
            DoseUnit = doseUnit,
            Route = route,
            VetUserId = vetUserId,
            BatchNumber = batchNumber,
            Expiry = expiry,
            WithdrawalUntil = withdrawalUntil,
            NextDueDate = nextDue,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
        };
    }
}

public sealed record HealthEventRecordedEvent(
    Guid HealthEventId,
    Guid OrganisationId,
    Guid AnimalId,
    HealthEventType EventType,
    DateOnly? WithdrawalUntil,
    DateTimeOffset OccurredAt) : IDomainEvent;
