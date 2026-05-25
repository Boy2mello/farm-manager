using FarmManager.Application.Flagging;

namespace FarmManager.Application.Analytics;

public interface IMetricsCalculator
{
    /// <summary>
    /// Builds the metrics snapshot for one cow on a given as-of date. Returns null for non-breeding
    /// animals or when there isn't enough history.
    /// </summary>
    Task<CowPerformanceMetrics?> ComputeForCowAsync(Guid animalId, DateOnly asOf, CancellationToken ct = default);

    /// <summary>
    /// Computes the herd-level KPIs from spec §9.3 for the supplied organisation and as-of date.
    /// </summary>
    Task<HerdKpis> ComputeHerdAsync(Guid organisationId, DateOnly asOf, CancellationToken ct = default);
}

public sealed record HerdKpis(
    int LiveCattle,
    int ConfirmedPregnancies,
    int CalvesYtd,
    decimal CalvingRate,
    decimal MortalityRateYtd,
    decimal AvgCalvingIntervalMonths,
    decimal CalfMortalityRate30d,
    int TierA,
    int TierB,
    int TierC,
    int TierD,
    int TierE,
    int ActiveCowsBelowTierC,
    int WatchList);
