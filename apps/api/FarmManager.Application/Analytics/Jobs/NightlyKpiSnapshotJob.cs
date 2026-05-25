using FarmManager.Application.Common.Interfaces;
using FarmManager.Domain.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmManager.Application.Analytics.Jobs;

/// <summary>
/// RULE-018 (spec §11.18). Persists today's herd KPI metrics with deltas vs same period last
/// month and last year. Idempotent — re-running for the same day overwrites the row via the
/// (organisation_id, as_of_date, metric_name) unique index.
/// </summary>
public sealed class NightlyKpiSnapshotJob(
    IFarmManagerDbContext db,
    IMetricsCalculator metrics,
    ILogger<NightlyKpiSnapshotJob> logger)
{
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);
        var orgs = await db.Organisations.Select(o => o.Id).ToListAsync(ct);

        foreach (var orgId in orgs)
        {
            var kpis = await metrics.ComputeHerdAsync(orgId, asOf, ct);
            logger.LogInformation("KPI snapshot org={Org}: {Live} live, {Preg} pregnant, {Calves} calves YTD",
                orgId, kpis.LiveCattle, kpis.ConfirmedPregnancies, kpis.CalvesYtd);

            await UpsertAsync(orgId, asOf, "live_cattle", kpis.LiveCattle, ct);
            await UpsertAsync(orgId, asOf, "confirmed_pregnancies", kpis.ConfirmedPregnancies, ct);
            await UpsertAsync(orgId, asOf, "calves_ytd", kpis.CalvesYtd, ct);
            await UpsertAsync(orgId, asOf, "calving_rate", kpis.CalvingRate, ct);
            await UpsertAsync(orgId, asOf, "mortality_rate_ytd", kpis.MortalityRateYtd, ct);
            await UpsertAsync(orgId, asOf, "avg_calving_interval_months", kpis.AvgCalvingIntervalMonths, ct);
            await UpsertAsync(orgId, asOf, "calf_mortality_30d", kpis.CalfMortalityRate30d, ct);
            await UpsertAsync(orgId, asOf, "tier_a", kpis.TierA, ct);
            await UpsertAsync(orgId, asOf, "tier_b", kpis.TierB, ct);
            await UpsertAsync(orgId, asOf, "tier_c", kpis.TierC, ct);
            await UpsertAsync(orgId, asOf, "tier_d", kpis.TierD, ct);
            await UpsertAsync(orgId, asOf, "tier_e", kpis.TierE, ct);
            await UpsertAsync(orgId, asOf, "watch_list", kpis.WatchList, ct);
        }
    }

    private async Task UpsertAsync(Guid orgId, DateOnly asOf, string metric, decimal value, CancellationToken ct)
    {
        var existing = await db.HerdKpiSnapshots
            .FirstOrDefaultAsync(s => s.OrganisationId == orgId && s.AsOfDate == asOf && s.MetricName == metric, ct);

        // Deltas — find the latest snapshots for the matching dates.
        decimal? dMonth = await DeltaAsync(orgId, asOf.AddMonths(-1), metric, value, ct);
        decimal? dYear = await DeltaAsync(orgId, asOf.AddYears(-1), metric, value, ct);

        if (existing is null)
        {
            db.HerdKpiSnapshots.Add(HerdKpiSnapshot.Of(orgId, null, asOf, metric, value, dMonth, dYear));
        }
        else
        {
            // Replace: simplest semantics under the unique index.
            db.HerdKpiSnapshots.Remove(existing);
            db.HerdKpiSnapshots.Add(HerdKpiSnapshot.Of(orgId, null, asOf, metric, value, dMonth, dYear));
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task<decimal?> DeltaAsync(Guid orgId, DateOnly anchor, string metric, decimal current, CancellationToken ct)
    {
        var historical = await db.HerdKpiSnapshots.AsNoTracking()
            .Where(s => s.OrganisationId == orgId && s.MetricName == metric && s.AsOfDate <= anchor)
            .OrderByDescending(s => s.AsOfDate)
            .Select(s => (decimal?)s.Value)
            .FirstOrDefaultAsync(ct);

        return historical is null ? null : current - historical;
    }
}
