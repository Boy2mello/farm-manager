namespace FarmManager.Domain.Analytics;

/// <summary>
/// Daily snapshot row per spec §18.1 / §11.18 (RULE-018).
/// </summary>
public sealed class HerdKpiSnapshot
{
    public Guid Id { get; private set; }
    public Guid OrganisationId { get; private set; }
    public Guid? FarmId { get; private set; }
    public DateOnly AsOfDate { get; private set; }
    public string MetricName { get; private set; } = default!;
    public decimal Value { get; private set; }
    public decimal? DeltaVsLastMonth { get; private set; }
    public decimal? DeltaVsLastYear { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private HerdKpiSnapshot() { }

    public static HerdKpiSnapshot Of(
        Guid organisationId,
        Guid? farmId,
        DateOnly asOf,
        string metricName,
        decimal value,
        decimal? dMonth,
        decimal? dYear) => new()
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisationId,
            FarmId = farmId,
            AsOfDate = asOf,
            MetricName = metricName,
            Value = value,
            DeltaVsLastMonth = dMonth,
            DeltaVsLastYear = dYear,
            CreatedAt = DateTimeOffset.UtcNow,
        };
}
