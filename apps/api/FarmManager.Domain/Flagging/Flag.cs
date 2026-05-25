namespace FarmManager.Domain.Flagging;

public enum FlagSeverity
{
    Info = 0,
    Low = 1,
    Medium = 2,
    High = 3,
}

/// <summary>
/// Catalogue of cow + bull flag codes per spec §10.3 and §10.6.
/// </summary>
public static class FlagCodes
{
    // Cow flags (§10.3)
    public const string LowCpy = "low_cpy";
    public const string LongCalvingInterval = "long_calving_interval";
    public const string LateFirstCalver = "late_first_calver";
    public const string RepeatBreeder = "repeat_breeder";
    public const string NeverCalvedOverdue = "never_calved_overdue";
    public const string CalfMortality = "calf_mortality";
    public const string MultipleCalfLosses = "multiple_calf_losses";
    public const string DystociaHistory = "dystocia_history";
    public const string LowBcs = "low_bcs";
    public const string WeightLoss = "weight_loss";
    public const string NotSeen = "not_seen";
    public const string OverduePregnancyCheck = "overdue_pregnancy_check";
    public const string OverdueCalving = "overdue_calving";
    public const string WithdrawalActive = "withdrawal_active";
    public const string InbreedingDescendant = "inbreeding_descendant";

    // Bull flags (§10.6)
    public const string LowConceptionRate = "low_conception_rate";
    public const string DaughtersUnderperform = "daughters_underperform";
    public const string InbreedingConstrained = "inbreeding_constrained";
    public const string RetirementAge = "retirement_age";
}

/// <summary>
/// A persisted, explainable flag against an animal (spec §10.7).
/// </summary>
public sealed class Flag
{
    public Guid Id { get; private set; }
    public Guid OrganisationId { get; private set; }
    public Guid AnimalId { get; private set; }
    public string Code { get; private set; } = default!;
    public FlagSeverity Severity { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public string Reason { get; private set; } = default!;
    public string MetricsJson { get; private set; } = "{}";
    public Guid? SourceEventId { get; private set; }

    private Flag() { }

    public static Flag Open(
        Guid organisationId,
        Guid animalId,
        string code,
        FlagSeverity severity,
        string reason,
        string metricsJson,
        Guid? sourceEventId = null) => new()
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisationId,
            AnimalId = animalId,
            Code = code,
            Severity = severity,
            AssignedAt = DateTimeOffset.UtcNow,
            Reason = reason,
            MetricsJson = metricsJson,
            SourceEventId = sourceEventId,
        };

    public void Resolve()
    {
        ResolvedAt ??= DateTimeOffset.UtcNow;
    }
}
