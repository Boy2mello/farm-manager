using FarmManager.Domain.Common;

namespace FarmManager.Domain.Breeding;

/// <summary>
/// Append-only event capturing a calving. Schema per spec §18.1.
/// </summary>
public sealed class CalvingEvent : Entity<Guid>
{
    public Guid OrganisationId { get; private set; }
    public Guid DamId { get; private set; }
    public Guid CalfId { get; private set; }
    public Guid? SireId { get; private set; }
    public string? SireExternalNote { get; private set; }

    public DateOnly CalvingDate { get; private set; }
    public int DifficultyScore { get; private set; } // 1–5
    public bool AssistanceRequired { get; private set; }
    public bool PlacentaDelivered { get; private set; }
    public int? MotheringAbility { get; private set; } // 1–5

    public bool Stillbirth { get; private set; }
    public decimal? CalfWeightKg { get; private set; }
    public int? CalfVigour { get; private set; } // 1–5
    public string? Notes { get; private set; }
    public string AttachmentsJson { get; private set; } = "[]";

    private CalvingEvent() { }

    public static CalvingEvent Create(
        Guid organisationId,
        Guid damId,
        Guid calfId,
        DateOnly calvingDate,
        Guid? sireId,
        string? sireExternalNote,
        int difficultyScore,
        bool assistanceRequired,
        bool placentaDelivered,
        int? motheringAbility,
        bool stillbirth,
        decimal? calfWeightKg,
        int? calfVigour,
        string? notes,
        string? createdBy)
    {
        if (difficultyScore is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(difficultyScore), "Difficulty must be 1–5.");
        }

        if (motheringAbility is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(motheringAbility), "Mothering ability must be 1–5.");
        }

        return new CalvingEvent
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisationId,
            DamId = damId,
            CalfId = calfId,
            SireId = sireId,
            SireExternalNote = sireExternalNote,
            CalvingDate = calvingDate,
            DifficultyScore = difficultyScore,
            AssistanceRequired = assistanceRequired,
            PlacentaDelivered = placentaDelivered,
            MotheringAbility = motheringAbility,
            Stillbirth = stillbirth,
            CalfWeightKg = calfWeightKg,
            CalfVigour = calfVigour,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
        };
    }
}

public sealed record CalvingRecordedEvent(
    Guid CalvingEventId,
    Guid OrganisationId,
    Guid DamId,
    Guid CalfId,
    string CalfCodeName,
    bool Stillbirth,
    DateTimeOffset OccurredAt) : IDomainEvent;
