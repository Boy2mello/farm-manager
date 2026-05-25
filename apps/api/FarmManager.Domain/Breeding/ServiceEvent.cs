using FarmManager.Domain.Common;

namespace FarmManager.Domain.Breeding;

public enum ServiceType
{
    NaturalBull = 1,
    ArtificialInsemination = 2,
    EmbryoTransfer = 3,
}

public sealed class ServiceEvent : Entity<Guid>
{
    public const int GestationDays = 283; // Spec Appendix B.

    public Guid OrganisationId { get; private set; }
    public Guid CowId { get; private set; }
    public Guid? BullId { get; private set; }
    public Guid? AiStrawId { get; private set; }
    public DateOnly ServiceDate { get; private set; }
    public ServiceType ServiceType { get; private set; }
    public DateOnly ExpectedCalvingDate { get; private set; }
    public decimal? InbreedingCoefficient { get; private set; }
    public string? Notes { get; private set; }

    private ServiceEvent() { }

    public static ServiceEvent Create(
        Guid organisationId,
        Guid cowId,
        DateOnly serviceDate,
        ServiceType type,
        Guid? bullId,
        Guid? aiStrawId,
        decimal? inbreedingCoefficient,
        string? notes,
        string? createdBy) => new()
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisationId,
            CowId = cowId,
            BullId = bullId,
            AiStrawId = aiStrawId,
            ServiceDate = serviceDate,
            ServiceType = type,
            ExpectedCalvingDate = serviceDate.AddDays(GestationDays),
            InbreedingCoefficient = inbreedingCoefficient,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
        };
}

public sealed record MatingRecordedEvent(
    Guid ServiceEventId,
    Guid OrganisationId,
    Guid CowId,
    Guid? BullId,
    DateOnly ExpectedCalvingDate,
    DateTimeOffset OccurredAt) : IDomainEvent;
