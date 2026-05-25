using FarmManager.Domain.Common;

namespace FarmManager.Domain.Breeding;

public enum PregnancyCheckMethod
{
    Palpation = 1,
    Ultrasound = 2,
    Blood = 3,
    Visual = 4,
}

public enum PregnancyCheckResult
{
    Positive = 1,
    Negative = 2,
    Inconclusive = 3,
}

public sealed class PregnancyCheckEvent : Entity<Guid>
{
    public Guid OrganisationId { get; private set; }
    public Guid CowId { get; private set; }
    public DateOnly CheckDate { get; private set; }
    public PregnancyCheckMethod Method { get; private set; }
    public PregnancyCheckResult Result { get; private set; }
    public int? DaysBred { get; private set; }
    public Guid? VetUserId { get; private set; }
    public string? Notes { get; private set; }

    private PregnancyCheckEvent() { }

    public static PregnancyCheckEvent Create(
        Guid organisationId,
        Guid cowId,
        DateOnly checkDate,
        PregnancyCheckMethod method,
        PregnancyCheckResult result,
        int? daysBred,
        Guid? vetUserId,
        string? notes,
        string? createdBy) => new()
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisationId,
            CowId = cowId,
            CheckDate = checkDate,
            Method = method,
            Result = result,
            DaysBred = daysBred,
            VetUserId = vetUserId,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
        };
}

public sealed record PregnancyConfirmedEvent(
    Guid PregnancyCheckEventId,
    Guid OrganisationId,
    Guid CowId,
    DateOnly CheckDate,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record PregnancyFailedEvent(
    Guid PregnancyCheckEventId,
    Guid OrganisationId,
    Guid CowId,
    DateOnly CheckDate,
    DateTimeOffset OccurredAt) : IDomainEvent;
