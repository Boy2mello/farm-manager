using FarmManager.Application.Common.Interfaces;
using FarmManager.Application.Flagging;
using FarmManager.Domain.Animals;
using FarmManager.Domain.Breeding;
using Microsoft.EntityFrameworkCore;

namespace FarmManager.Application.Analytics;

/// <summary>
/// Computes the metrics in spec Appendix F from the event stream. Phase D performance: nightly
/// recalc only; Phase E adds materialised views and Redis caching.
/// </summary>
public sealed class MetricsCalculator(IFarmManagerDbContext db) : IMetricsCalculator
{
    private const decimal DaysInMonth = 30.4375m;

    public async Task<CowPerformanceMetrics?> ComputeForCowAsync(Guid animalId, DateOnly asOf, CancellationToken ct = default)
    {
        var cow = await db.Animals.AsNoTracking().FirstOrDefaultAsync(a => a.Id == animalId, ct);
        if (cow is null || cow.Sex != AnimalSex.Female) return null;

        var calvings = await db.CalvingEvents.AsNoTracking()
            .Where(c => c.DamId == animalId)
            .OrderBy(c => c.CalvingDate)
            .ToListAsync(ct);

        var calves = await db.Animals.AsNoTracking()
            .Where(a => a.DamId == animalId)
            .Select(a => new { a.Id, a.Status, a.Dob })
            .ToListAsync(ct);

        var ageDays = asOf.DayNumber - cow.Dob.DayNumber;
        var ageYears = (decimal)ageDays / 365.25m;
        var productiveYears = Math.Max(0m, ageYears - 2m);

        var calvesAlive = calves.Count(c => c.Status != AnimalStatus.Dead);
        var calfLosses = calves.Count(c => c.Status == AnimalStatus.Dead);

        decimal? cpy = productiveYears > 0 ? Math.Round(calvings.Count / productiveYears, 3) : null;

        decimal? avgInterval = null;
        decimal? lastInterval = null;
        if (calvings.Count >= 2)
        {
            var gaps = new List<int>();
            for (int i = 1; i < calvings.Count; i++)
            {
                gaps.Add(calvings[i].CalvingDate.DayNumber - calvings[i - 1].CalvingDate.DayNumber);
            }
            avgInterval = Math.Round((decimal)gaps.Average() / DaysInMonth, 2);
            lastInterval = Math.Round((decimal)gaps[^1] / DaysInMonth, 2);
        }

        decimal? monthsSinceLast = cow.LastCalvingDate is null
            ? null
            : Math.Round((decimal)(asOf.DayNumber - cow.LastCalvingDate.Value.DayNumber) / DaysInMonth, 2);

        decimal? ageAtFirstCalving = calvings.Count == 0
            ? null
            : Math.Round((decimal)(calvings[0].CalvingDate.DayNumber - cow.Dob.DayNumber) / DaysInMonth, 2);

        // Services without confirmed pregnancy since the last calving (or ever).
        var sinceDate = cow.LastCalvingDate ?? cow.Dob;
        var services = await db.ServiceEvents.AsNoTracking()
            .CountAsync(s => s.CowId == animalId && s.ServiceDate > sinceDate, ct);
        var confirmed = await db.PregnancyCheckEvents.AsNoTracking()
            .CountAsync(p => p.CowId == animalId && p.CheckDate > sinceDate && p.Result == PregnancyCheckResult.Positive, ct);
        var servicesWithoutPreg = Math.Max(0, services - confirmed);

        var dystocia = calvings.LastOrDefault()?.DifficultyScore >= 4;

        var calfMortality30d = await db.Animals.AsNoTracking()
            .AnyAsync(a => a.DamId == animalId
                && a.Status == AnimalStatus.Dead
                && a.Dob >= asOf.AddDays(-30), ct);

        var isFirstTimer = calvings.Count == 1 &&
            (asOf.DayNumber - calvings[0].CalvingDate.DayNumber) < 365;

        return new CowPerformanceMetrics(
            AnimalId: cow.Id,
            AgeYears: Math.Round(ageYears, 2),
            ProductiveYears: Math.Round(productiveYears, 2),
            TotalCalves: calvings.Count,
            CalvesAlive: calvesAlive,
            CalfLossesLifetime: calfLosses,
            Cpy: cpy,
            AvgCalvingIntervalMonths: avgInterval,
            LastCalvingIntervalMonths: lastInterval,
            MonthsSinceLastCalving: monthsSinceLast,
            AgeAtFirstCalvingMonths: ageAtFirstCalving,
            ServicesWithoutConfirmedPregnancy: servicesWithoutPreg,
            DystociaLastCalving: dystocia,
            CalfMortalityLast30d: calfMortality30d,
            IsFirstTimeMotherThisCycle: isFirstTimer);
    }

    public async Task<HerdKpis> ComputeHerdAsync(Guid organisationId, DateOnly asOf, CancellationToken ct = default)
    {
        var yearStart = new DateOnly(asOf.Year, 1, 1);

        var animals = db.Animals.AsNoTracking().Where(a => a.OrganisationId == organisationId);

        var live = await animals.CountAsync(a =>
            a.Status != AnimalStatus.Dead
            && a.Status != AnimalStatus.Sold
            && a.Status != AnimalStatus.Transferred, ct);

        var confirmedPreg = await animals.CountAsync(a => a.Status == AnimalStatus.PregnantConfirmed, ct);

        var calvesYtd = await db.CalvingEvents.AsNoTracking()
            .CountAsync(c => c.OrganisationId == organisationId && c.CalvingDate >= yearStart && c.CalvingDate <= asOf, ct);

        var exposedYtd = await db.ServiceEvents.AsNoTracking()
            .Select(s => s.CowId)
            .Distinct()
            .CountAsync(ct);

        decimal calvingRate = exposedYtd == 0 ? 0 : Math.Round((decimal)calvesYtd / exposedYtd, 3);

        var startCount = await animals.CountAsync(a => a.Dob <= yearStart, ct);
        var deathsYtd = await animals.CountAsync(a =>
            a.Status == AnimalStatus.Dead
            && a.DisposalDate >= yearStart, ct);
        var mortality = startCount == 0 ? 0m : Math.Round((decimal)deathsYtd / startCount, 4);

        // Average calving interval (months) — pooled from cows with ≥ 2 calvings.
        var intervalGaps = await db.CalvingEvents.AsNoTracking()
            .Where(c => c.OrganisationId == organisationId)
            .GroupBy(c => c.DamId)
            .Where(g => g.Count() >= 2)
            .Select(g => g.OrderBy(x => x.CalvingDate).Select(x => x.CalvingDate).ToList())
            .ToListAsync(ct);

        decimal avgInterval = 0m;
        var totalGaps = 0;
        var sumGaps = 0;
        foreach (var dates in intervalGaps)
        {
            for (int i = 1; i < dates.Count; i++)
            {
                sumGaps += dates[i].DayNumber - dates[i - 1].DayNumber;
                totalGaps++;
            }
        }
        if (totalGaps > 0) avgInterval = Math.Round(((decimal)sumGaps / totalGaps) / DaysInMonth, 2);

        var thirtyDayDeaths = await animals.CountAsync(a =>
            a.Status == AnimalStatus.Dead
            && a.Dob >= asOf.AddDays(-30), ct);
        var liveBirths = await db.CalvingEvents.AsNoTracking()
            .CountAsync(c => c.OrganisationId == organisationId
                && !c.Stillbirth
                && c.CalvingDate >= yearStart, ct);
        decimal calfMortality = liveBirths == 0 ? 0m : Math.Round((decimal)thirtyDayDeaths / liveBirths, 4);

        var tierCounts = await animals
            .GroupBy(a => a.PerformanceTier)
            .Select(g => new { Tier = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Tier, x => x.Count, ct);

        int Count(PerformanceTier t) => tierCounts.TryGetValue(t, out var c) ? c : 0;

        var watch = Count(PerformanceTier.D) + Count(PerformanceTier.E);

        return new HerdKpis(
            LiveCattle: live,
            ConfirmedPregnancies: confirmedPreg,
            CalvesYtd: calvesYtd,
            CalvingRate: calvingRate,
            MortalityRateYtd: mortality,
            AvgCalvingIntervalMonths: avgInterval,
            CalfMortalityRate30d: calfMortality,
            TierA: Count(PerformanceTier.A),
            TierB: Count(PerformanceTier.B),
            TierC: Count(PerformanceTier.C),
            TierD: Count(PerformanceTier.D),
            TierE: Count(PerformanceTier.E),
            ActiveCowsBelowTierC: Count(PerformanceTier.D) + Count(PerformanceTier.E),
            WatchList: watch);
    }
}
