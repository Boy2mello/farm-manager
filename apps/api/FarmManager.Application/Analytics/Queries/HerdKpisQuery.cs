using FarmManager.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmManager.Application.Analytics.Queries;

public sealed record KpiValue(string Metric, decimal Value, decimal? DeltaMonth, decimal? DeltaYear, DateOnly AsOfDate);

public sealed record HerdKpisQuery() : IRequest<IReadOnlyList<KpiValue>>;

public sealed class HerdKpisHandler(IFarmManagerDbContext db, ICurrentUser currentUser, IMetricsCalculator metrics)
    : IRequestHandler<HerdKpisQuery, IReadOnlyList<KpiValue>>
{
    public async Task<IReadOnlyList<KpiValue>> Handle(HerdKpisQuery request, CancellationToken ct)
    {
        var orgId = currentUser.OrganisationId
            ?? throw new InvalidOperationException("No organisation context.");

        // Use the latest snapshot if present; otherwise compute on demand.
        var latest = await db.HerdKpiSnapshots.AsNoTracking()
            .Where(s => s.OrganisationId == orgId)
            .OrderByDescending(s => s.AsOfDate)
            .Take(64)
            .ToListAsync(ct);

        if (latest.Count > 0)
        {
            return latest
                .GroupBy(s => s.MetricName)
                .Select(g => g.OrderByDescending(x => x.AsOfDate).First())
                .Select(s => new KpiValue(s.MetricName, s.Value, s.DeltaVsLastMonth, s.DeltaVsLastYear, s.AsOfDate))
                .ToList();
        }

        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);
        var live = await metrics.ComputeHerdAsync(orgId, asOf, ct);

        return new[]
        {
            new KpiValue("live_cattle", live.LiveCattle, null, null, asOf),
            new KpiValue("confirmed_pregnancies", live.ConfirmedPregnancies, null, null, asOf),
            new KpiValue("calves_ytd", live.CalvesYtd, null, null, asOf),
            new KpiValue("calving_rate", live.CalvingRate, null, null, asOf),
            new KpiValue("mortality_rate_ytd", live.MortalityRateYtd, null, null, asOf),
            new KpiValue("avg_calving_interval_months", live.AvgCalvingIntervalMonths, null, null, asOf),
            new KpiValue("calf_mortality_30d", live.CalfMortalityRate30d, null, null, asOf),
            new KpiValue("tier_a", live.TierA, null, null, asOf),
            new KpiValue("tier_b", live.TierB, null, null, asOf),
            new KpiValue("tier_c", live.TierC, null, null, asOf),
            new KpiValue("tier_d", live.TierD, null, null, asOf),
            new KpiValue("tier_e", live.TierE, null, null, asOf),
            new KpiValue("watch_list", live.WatchList, null, null, asOf),
        };
    }
}

public sealed record TrendPoint(DateOnly AsOfDate, decimal Value);
public sealed record MetricTrendQuery(string Metric, int Days = 90) : IRequest<IReadOnlyList<TrendPoint>>;

public sealed class MetricTrendHandler(IFarmManagerDbContext db, ICurrentUser currentUser)
    : IRequestHandler<MetricTrendQuery, IReadOnlyList<TrendPoint>>
{
    public async Task<IReadOnlyList<TrendPoint>> Handle(MetricTrendQuery request, CancellationToken ct)
    {
        var orgId = currentUser.OrganisationId
            ?? throw new InvalidOperationException("No organisation context.");
        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-request.Days);

        return await db.HerdKpiSnapshots.AsNoTracking()
            .Where(s => s.OrganisationId == orgId && s.MetricName == request.Metric && s.AsOfDate >= cutoff)
            .OrderBy(s => s.AsOfDate)
            .Select(s => new TrendPoint(s.AsOfDate, s.Value))
            .ToListAsync(ct);
    }
}

public sealed record UnderperformerRow(
    Guid AnimalId,
    string CodeName,
    string? PrimaryName,
    int Tier,
    IReadOnlyList<string> FlagCodes);

public sealed record UnderperformersQuery(string? FlagFilter = null) : IRequest<IReadOnlyList<UnderperformerRow>>;

public sealed class UnderperformersHandler(IFarmManagerDbContext db, ICurrentUser currentUser)
    : IRequestHandler<UnderperformersQuery, IReadOnlyList<UnderperformerRow>>
{
    public async Task<IReadOnlyList<UnderperformerRow>> Handle(UnderperformersQuery request, CancellationToken ct)
    {
        var orgId = currentUser.OrganisationId
            ?? throw new InvalidOperationException("No organisation context.");

        var flagsByAnimal = await db.Flags.AsNoTracking()
            .Where(f => f.OrganisationId == orgId && f.ResolvedAt == null
                && (request.FlagFilter == null || f.Code == request.FlagFilter))
            .GroupBy(f => f.AnimalId)
            .Select(g => new { AnimalId = g.Key, Codes = g.Select(x => x.Code).ToList() })
            .ToListAsync(ct);

        if (flagsByAnimal.Count == 0) return Array.Empty<UnderperformerRow>();

        var ids = flagsByAnimal.Select(x => x.AnimalId).ToHashSet();
        var animals = await db.Animals.AsNoTracking()
            .Where(a => ids.Contains(a.Id))
            .Select(a => new { a.Id, a.CodeName, a.PrimaryName, a.PerformanceTier })
            .ToListAsync(ct);

        return flagsByAnimal
            .Join(animals, f => f.AnimalId, a => a.Id, (f, a) => new UnderperformerRow(
                a.Id, a.CodeName, a.PrimaryName, (int)a.PerformanceTier, f.Codes))
            .OrderByDescending(r => r.Tier)
            .ToList();
    }
}
