using FarmManager.Domain.Animals;

namespace FarmManager.Domain.Flagging;

/// <summary>
/// Audit row written whenever a cow's performance tier changes (spec §10.4, §11.7).
/// </summary>
public sealed class TierAssignment
{
    public Guid Id { get; private set; }
    public Guid OrganisationId { get; private set; }
    public Guid AnimalId { get; private set; }
    public PerformanceTier PreviousTier { get; private set; }
    public PerformanceTier Tier { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }
    public string AssignedBy { get; private set; } = "system";
    public string Reason { get; private set; } = default!;
    public string MetricsJson { get; private set; } = "{}";

    private TierAssignment() { }

    public static TierAssignment Record(
        Guid organisationId,
        Guid animalId,
        PerformanceTier previous,
        PerformanceTier current,
        string reason,
        string metricsJson,
        string assignedBy = "system") => new()
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisationId,
            AnimalId = animalId,
            PreviousTier = previous,
            Tier = current,
            AssignedAt = DateTimeOffset.UtcNow,
            AssignedBy = assignedBy,
            Reason = reason,
            MetricsJson = metricsJson,
        };
}
