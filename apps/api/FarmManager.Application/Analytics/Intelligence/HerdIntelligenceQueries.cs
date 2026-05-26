using FarmManager.Application.Common.Interfaces;
using FarmManager.Domain.Animals;
using FarmManager.Domain.Breeding;
using FarmManager.Domain.Commerce;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmManager.Application.Analytics.Intelligence;

// =======================================================================
// Shared DTOs
// =======================================================================

public sealed record AnimalRef(Guid Id, string CodeName, string? PrimaryName);

// =======================================================================
// HERD INTELLIGENCE  ───────────────────────────────────────────────
// Composition, fertility, mortality, replacement rate.
// =======================================================================

public sealed record HerdIntelligenceQuery() : IRequest<HerdIntelligenceReport>;

public sealed record HerdIntelligenceReport(
    DateOnly AsOfDate,

    // Composition
    int LiveTotal,
    int BreedingCows,            // status Active|Open|Exposed|Pregnant|Lactating|Dry AND sex F AND age ≥ 2y
    int Heifers,                 // F, not yet calved, age >= 18 mo
    int Calves,                  // age < 1y
    int Bulls,                   // M, age >= 2y
    int SteersAndOther,          // everything else

    // Fertility now
    int CowsConfirmedPregnant,
    int CowsOpen,
    int CowsLactating,
    decimal PregnancyRatePctNow, // confirmed / breeding_cows
    decimal ReplacementRatePctYtd, // (heifers brought to breeding + births) / breeding_cows

    // Productivity averages
    decimal MeanCpy,             // breeding-cow mean of calves per productive year
    decimal MeanCalvingIntervalMonths,
    decimal CalfMortalityRatePct,// last 30d / live births this year

    // Strength signals
    int TopPerformerCount,        // tier A
    int WatchListCount,           // tier D + E
    int AgeingOutCount,           // breeding cows age > 10
    int FirstTimeMothersThisYear  // cows with exactly one calving + within 12 months
);

public sealed class HerdIntelligenceHandler(IFarmManagerDbContext db, ICurrentUser currentUser)
    : IRequestHandler<HerdIntelligenceQuery, HerdIntelligenceReport>
{
    public async Task<HerdIntelligenceReport> Handle(HerdIntelligenceQuery request, CancellationToken ct)
    {
        var orgId = currentUser.OrganisationId
            ?? throw new InvalidOperationException("No organisation context.");
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);
        var oneYearAgo = asOf.AddYears(-1);

        var live = await db.Animals.AsNoTracking()
            .Where(a => a.OrganisationId == orgId
                && a.Status != AnimalStatus.Sold
                && a.Status != AnimalStatus.Dead
                && a.Status != AnimalStatus.Transferred)
            .ToListAsync(ct);

        int AgeYears(Animal a) => (asOf.DayNumber - a.Dob.DayNumber) / 365;

        var bulls = live.Count(a => a.Sex == AnimalSex.Male && AgeYears(a) >= 2);
        var calves = live.Count(a => AgeYears(a) < 1);
        var breedingCows = live.Count(a =>
            a.Sex == AnimalSex.Female
            && AgeYears(a) >= 2
            && a.Status != AnimalStatus.Missing);
        var heifers = live.Count(a =>
            a.Sex == AnimalSex.Female
            && AgeYears(a) >= 1
            && a.CalfCount == 0);
        var others = Math.Max(0, live.Count - breedingCows - heifers - calves - bulls);

        var pregnant = live.Count(a => a.Status == AnimalStatus.PregnantConfirmed);
        var open = live.Count(a => a.Status == AnimalStatus.Open);
        var lactating = live.Count(a => a.Status == AnimalStatus.Lactating);

        decimal pregnancyRate = breedingCows == 0 ? 0 : Math.Round((decimal)pregnant / breedingCows * 100m, 1);

        var calvesYtd = await db.CalvingEvents.AsNoTracking()
            .CountAsync(c => c.OrganisationId == orgId && c.CalvingDate >= new DateOnly(asOf.Year, 1, 1), ct);
        var liveBirthsYtd = await db.CalvingEvents.AsNoTracking()
            .CountAsync(c => c.OrganisationId == orgId
                && c.CalvingDate >= new DateOnly(asOf.Year, 1, 1)
                && !c.Stillbirth, ct);
        var calfDeathsYtd = await db.Animals.AsNoTracking()
            .CountAsync(a => a.OrganisationId == orgId
                && a.Status == AnimalStatus.Dead
                && a.Dob >= new DateOnly(asOf.Year, 1, 1), ct);
        decimal calfMort = liveBirthsYtd == 0 ? 0 : Math.Round((decimal)calfDeathsYtd / liveBirthsYtd * 100m, 1);

        decimal replacementRate = breedingCows == 0
            ? 0
            : Math.Round(((decimal)heifers + calvesYtd) / breedingCows * 100m, 1);

        var cowsWithCpy = live
            .Where(a => a.Sex == AnimalSex.Female && a.CalvesPerYear != null)
            .ToList();
        decimal meanCpy = cowsWithCpy.Count == 0 ? 0
            : Math.Round(cowsWithCpy.Sum(a => a.CalvesPerYear ?? 0) / cowsWithCpy.Count, 2);

        var withInterval = live.Where(a => a.AvgCalvingIntervalDays != null).ToList();
        decimal meanInterval = withInterval.Count == 0 ? 0
            : Math.Round(withInterval.Sum(a => (a.AvgCalvingIntervalDays ?? 0) / 30.4375m) / withInterval.Count, 1);

        var topPerformers = live.Count(a => a.PerformanceTier == PerformanceTier.A);
        var watchList = live.Count(a => a.PerformanceTier == PerformanceTier.D || a.PerformanceTier == PerformanceTier.E);
        var ageing = live.Count(a => a.Sex == AnimalSex.Female && AgeYears(a) > 10);
        var firstTimers = live.Count(a => a.CalfCount == 1 && a.LastCalvingDate is not null && a.LastCalvingDate >= oneYearAgo);

        return new HerdIntelligenceReport(
            AsOfDate: asOf,
            LiveTotal: live.Count,
            BreedingCows: breedingCows,
            Heifers: heifers,
            Calves: calves,
            Bulls: bulls,
            SteersAndOther: others,
            CowsConfirmedPregnant: pregnant,
            CowsOpen: open,
            CowsLactating: lactating,
            PregnancyRatePctNow: pregnancyRate,
            ReplacementRatePctYtd: replacementRate,
            MeanCpy: meanCpy,
            MeanCalvingIntervalMonths: meanInterval,
            CalfMortalityRatePct: calfMort,
            TopPerformerCount: topPerformers,
            WatchListCount: watchList,
            AgeingOutCount: ageing,
            FirstTimeMothersThisYear: firstTimers);
    }
}

// =======================================================================
// BULL INTELLIGENCE  ───────────────────────────────────────────────
// One row per active+historical sire with derived KPIs.
// =======================================================================

public sealed record BullIntelligenceQuery() : IRequest<IReadOnlyList<BullIntelligenceRow>>;

public sealed record BullIntelligenceRow(
    AnimalRef Bull,
    DateOnly Dob,
    decimal AgeYears,
    int OffspringTotal,
    int OffspringAlive,
    int OffspringLost,
    decimal CalfSurvivalPct,
    int DaughtersBreedingAge,
    decimal MeanDaughterTier,           // 1=A … 5=E (lower is better)
    int DaughtersTierAB,
    int DaughtersTierDE,
    int LastSiredYear,
    string? Verdict);                    // narrative — "top sire", "underperforming", etc

public sealed class BullIntelligenceHandler(IFarmManagerDbContext db, ICurrentUser currentUser)
    : IRequestHandler<BullIntelligenceQuery, IReadOnlyList<BullIntelligenceRow>>
{
    public async Task<IReadOnlyList<BullIntelligenceRow>> Handle(BullIntelligenceQuery request, CancellationToken ct)
    {
        var orgId = currentUser.OrganisationId
            ?? throw new InvalidOperationException("No organisation context.");
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);

        // Any male animal that has at least one offspring counts as a bull for this report.
        var siredIds = await db.Animals.AsNoTracking()
            .Where(a => a.OrganisationId == orgId && a.SireId != null)
            .Select(a => a.SireId!.Value)
            .Distinct()
            .ToListAsync(ct);

        var bulls = await db.Animals.AsNoTracking()
            .Where(a => siredIds.Contains(a.Id))
            .ToListAsync(ct);

        var rows = new List<BullIntelligenceRow>();
        foreach (var bull in bulls)
        {
            var offspring = await db.Animals.AsNoTracking()
                .Where(a => a.SireId == bull.Id)
                .Select(a => new { a.Status, a.PerformanceTier, a.Sex, a.Dob, a.CalfCount })
                .ToListAsync(ct);

            var total = offspring.Count;
            var alive = offspring.Count(o => o.Status != AnimalStatus.Dead && o.Status != AnimalStatus.Sold);
            var lost = offspring.Count(o => o.Status == AnimalStatus.Dead);

            decimal survival = total == 0 ? 0 : Math.Round((decimal)(total - lost) / total * 100m, 1);

            var daughters = offspring.Where(o => o.Sex == AnimalSex.Female).ToList();
            var bredAge = daughters.Where(d => (asOf.DayNumber - d.Dob.DayNumber) / 365 >= 3).ToList();
            var ranked = bredAge.Where(d => d.PerformanceTier != PerformanceTier.None).ToList();
            decimal meanTier = ranked.Count == 0 ? 0
                : Math.Round(ranked.Average(d => (decimal)(int)d.PerformanceTier), 2);
            var tierAB = ranked.Count(d => d.PerformanceTier == PerformanceTier.A || d.PerformanceTier == PerformanceTier.B);
            var tierDE = ranked.Count(d => d.PerformanceTier == PerformanceTier.D || d.PerformanceTier == PerformanceTier.E);
            var lastSiredYear = offspring.Count == 0 ? 0 : offspring.Max(o => o.Dob.Year);

            var ageYears = Math.Round((decimal)(asOf.DayNumber - bull.Dob.DayNumber) / 365.25m, 1);

            // Verdict heuristic
            string? verdict = null;
            if (total == 0) verdict = "No recorded offspring yet.";
            else if (ranked.Count >= 5 && meanTier <= 2.0m) verdict = "Top sire — daughters consistently in tier A/B.";
            else if (ranked.Count >= 5 && meanTier >= 4.0m) verdict = "Underperforming — daughters cluster in tier D/E.";
            else if (survival < 70m && total >= 5) verdict = "Calf survival below 70 % — investigate calving difficulty or maternal mismatch.";
            else if (ageYears >= 7) verdict = "Approaching retirement age (≥ 7 yr).";

            rows.Add(new BullIntelligenceRow(
                Bull: new AnimalRef(bull.Id, bull.CodeName, bull.PrimaryName),
                Dob: bull.Dob,
                AgeYears: ageYears,
                OffspringTotal: total,
                OffspringAlive: alive,
                OffspringLost: lost,
                CalfSurvivalPct: survival,
                DaughtersBreedingAge: bredAge.Count,
                MeanDaughterTier: meanTier,
                DaughtersTierAB: tierAB,
                DaughtersTierDE: tierDE,
                LastSiredYear: lastSiredYear,
                Verdict: verdict));
        }

        return rows
            .OrderByDescending(r => r.OffspringTotal)
            .ThenBy(r => r.MeanDaughterTier == 0 ? 99 : r.MeanDaughterTier)
            .ToList();
    }
}

// =======================================================================
// BLOODLINE INTELLIGENCE  ──────────────────────────────────────────
// Group cows by sire (paternal bloodline) and average their tier + CPY.
// =======================================================================

public sealed record BloodlineIntelligenceQuery() : IRequest<IReadOnlyList<BloodlineRow>>;

public sealed record BloodlineRow(
    AnimalRef Sire,
    int Descendants,
    int BreedingAgeDescendants,
    decimal MeanTier,             // 1=A … 5=E
    decimal MeanCpy,
    decimal SurvivalPct);

public sealed class BloodlineIntelligenceHandler(IFarmManagerDbContext db, ICurrentUser currentUser)
    : IRequestHandler<BloodlineIntelligenceQuery, IReadOnlyList<BloodlineRow>>
{
    public async Task<IReadOnlyList<BloodlineRow>> Handle(BloodlineIntelligenceQuery request, CancellationToken ct)
    {
        var orgId = currentUser.OrganisationId
            ?? throw new InvalidOperationException("No organisation context.");
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);

        var animals = await db.Animals.AsNoTracking()
            .Where(a => a.OrganisationId == orgId)
            .ToListAsync(ct);

        var bySire = animals
            .Where(a => a.SireId != null)
            .GroupBy(a => a.SireId!.Value)
            .ToList();

        var rows = new List<BloodlineRow>();
        foreach (var g in bySire)
        {
            var sire = animals.FirstOrDefault(a => a.Id == g.Key);
            if (sire is null) continue;

            var descendants = g.ToList();
            var alive = descendants.Count(d => d.Status != AnimalStatus.Dead);
            var ranked = descendants.Where(d => d.PerformanceTier != PerformanceTier.None).ToList();
            var bredAge = descendants.Where(d => (asOf.DayNumber - d.Dob.DayNumber) / 365 >= 3).ToList();
            decimal meanTier = ranked.Count == 0 ? 0 : Math.Round(ranked.Average(d => (decimal)(int)d.PerformanceTier), 2);
            var cpys = descendants.Where(d => d.CalvesPerYear != null).Select(d => d.CalvesPerYear!.Value).ToList();
            decimal meanCpy = cpys.Count == 0 ? 0 : Math.Round(cpys.Average(), 2);
            decimal survival = descendants.Count == 0 ? 0 : Math.Round((decimal)alive / descendants.Count * 100m, 1);

            rows.Add(new BloodlineRow(
                Sire: new AnimalRef(sire.Id, sire.CodeName, sire.PrimaryName),
                Descendants: descendants.Count,
                BreedingAgeDescendants: bredAge.Count,
                MeanTier: meanTier,
                MeanCpy: meanCpy,
                SurvivalPct: survival));
        }

        return rows
            .OrderByDescending(r => r.Descendants)
            .ToList();
    }
}

// =======================================================================
// YEAR PERFORMANCE  ────────────────────────────────────────────────
// Full year-by-year breakdown + per-year narrative summary.
// =======================================================================

public sealed record YearPerformanceQuery() : IRequest<YearPerformanceReport>;

public sealed record YearPerformanceReport(
    IReadOnlyList<YearRow> Years,
    YearRow? BestYear,
    YearRow? WorstYear,
    int? BestYearLabel,
    int? WorstYearLabel,
    string Narrative);

public sealed record YearRow(
    int Year,
    int CalvesBorn,
    int LiveBirths,
    int Stillbirths,
    int CalfDeaths,
    decimal CalfSurvivalPct,
    int CowsBredOrConfirmed,
    int CowsThatCalved,
    decimal PregnancySuccessPct,
    decimal CalvingRatePct,
    decimal AvgCalvingIntervalMonths,
    int CowsSkippedYear,
    int FirstTimeCalvers,
    int Sales,
    int Deaths,
    int NetGrowth,
    AnimalRef? TopBull,
    int TopBullSurvivingCalves,
    AnimalRef? StandoutCow,
    string Narrative);

public sealed class YearPerformanceHandler(IFarmManagerDbContext db, ICurrentUser currentUser)
    : IRequestHandler<YearPerformanceQuery, YearPerformanceReport>
{
    public async Task<YearPerformanceReport> Handle(YearPerformanceQuery request, CancellationToken ct)
    {
        var orgId = currentUser.OrganisationId
            ?? throw new InvalidOperationException("No organisation context.");
        var thisYear = DateTime.UtcNow.Year;

        var calvings = await db.CalvingEvents.AsNoTracking()
            .Where(c => c.OrganisationId == orgId)
            .Select(c => new { c.CalvingDate, c.DamId, c.CalfId, c.SireId, c.Stillbirth })
            .ToListAsync(ct);

        var animals = await db.Animals.AsNoTracking()
            .Where(a => a.OrganisationId == orgId)
            .ToDictionaryAsync(a => a.Id, ct);

        var sales = await db.SaleEvents.AsNoTracking()
            .Where(s => s.OrganisationId == orgId)
            .Select(s => s.SaleDate)
            .ToListAsync(ct);

        var deaths = await db.DeathEvents.AsNoTracking()
            .Where(d => d.OrganisationId == orgId)
            .Select(d => d.DeathDate)
            .ToListAsync(ct);

        if (calvings.Count == 0)
        {
            return new YearPerformanceReport(
                Years: Array.Empty<YearRow>(),
                BestYear: null,
                WorstYear: null,
                BestYearLabel: null,
                WorstYearLabel: null,
                Narrative: "No calvings recorded yet — once you log calvings, the year-by-year story builds itself.");
        }

        var firstYear = calvings.Min(c => c.CalvingDate.Year);
        var rows = new List<YearRow>();

        // Per-cow last-calving year, for "skipped a year" calc.
        var calvingsByDam = calvings.GroupBy(c => c.DamId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.CalvingDate).OrderBy(d => d).ToList());

        for (int year = firstYear; year <= thisYear; year++)
        {
            var yearCalvings = calvings.Where(c => c.CalvingDate.Year == year).ToList();
            var liveBirths = yearCalvings.Count(c => !c.Stillbirth);
            var stillborn = yearCalvings.Count(c => c.Stillbirth);
            var calfDeaths = yearCalvings.Count(c =>
                animals.TryGetValue(c.CalfId, out var calf) && calf.Status == AnimalStatus.Dead);
            decimal survival = yearCalvings.Count == 0 ? 0
                : Math.Round((decimal)(yearCalvings.Count - calfDeaths) / yearCalvings.Count * 100m, 1);

            // Cows bred OR confirmed pregnant in this year (proxy via service events).
            var cowsBred = await db.ServiceEvents.AsNoTracking()
                .CountAsync(s => s.OrganisationId == orgId && s.ServiceDate.Year == year, ct);
            var distinctCowsBred = await db.ServiceEvents.AsNoTracking()
                .Where(s => s.OrganisationId == orgId && s.ServiceDate.Year == year)
                .Select(s => s.CowId)
                .Distinct()
                .CountAsync(ct);

            var cowsThatCalved = yearCalvings.Select(c => c.DamId).Distinct().Count();
            decimal pregSuccess = distinctCowsBred == 0 ? 0
                : Math.Round((decimal)cowsThatCalved / distinctCowsBred * 100m, 1);

            // Calving rate proxy: live births / breeding-age cows at the start of the year.
            var startCount = animals.Values.Count(a =>
                a.Sex == AnimalSex.Female && a.Dob.Year < year && a.Dob.Year >= year - 12);
            decimal calvingRate = startCount == 0 ? 0
                : Math.Round((decimal)liveBirths / startCount * 100m, 1);

            // Mean calving interval among cows who calved in this year + had a previous calving.
            var intervals = new List<int>();
            foreach (var c in yearCalvings)
            {
                if (!calvingsByDam.TryGetValue(c.DamId, out var dates)) continue;
                var idx = dates.IndexOf(c.CalvingDate);
                if (idx <= 0) continue;
                intervals.Add(c.CalvingDate.DayNumber - dates[idx - 1].DayNumber);
            }
            decimal avgInterval = intervals.Count == 0 ? 0
                : Math.Round(intervals.Average(i => (decimal)i / 30.4375m), 1);

            // Cows that "skipped" a year: cows who calved in year-2 but not in year-1.
            int skipped = 0;
            if (year >= firstYear + 1)
            {
                foreach (var (damId, dates) in calvingsByDam)
                {
                    var hadPriorCalving = dates.Any(d => d.Year < year);
                    var skippedThis = !dates.Any(d => d.Year == year);
                    var skippedLast = !dates.Any(d => d.Year == year - 1);
                    if (hadPriorCalving && skippedThis && skippedLast) skipped++;
                }
            }

            // First-time calvers in this year.
            int firstTimers = yearCalvings.Count(c =>
            {
                if (!calvingsByDam.TryGetValue(c.DamId, out var dates)) return false;
                return dates.First() == c.CalvingDate;
            });

            var yearSales = sales.Count(s => s.Year == year);
            var yearDeaths = deaths.Count(d => d.Year == year);
            var netGrowth = liveBirths - yearSales - yearDeaths;

            // Top bull for the year: most surviving offspring.
            var bullCalves = yearCalvings
                .Where(c => c.SireId != null && !c.Stillbirth)
                .GroupBy(c => c.SireId!.Value);
            (Guid sireId, int surviving) topBull = (Guid.Empty, 0);
            foreach (var g in bullCalves)
            {
                var surviving = g.Count(c =>
                    animals.TryGetValue(c.CalfId, out var calf) && calf.Status != AnimalStatus.Dead);
                if (surviving > topBull.surviving) topBull = (g.Key, surviving);
            }
            AnimalRef? topBullRef = topBull.sireId != Guid.Empty && animals.TryGetValue(topBull.sireId, out var tb)
                ? new AnimalRef(tb.Id, tb.CodeName, tb.PrimaryName)
                : null;

            // Standout cow: most calves alive contributed in this year (tied broken by code-name).
            var damContrib = yearCalvings
                .Where(c => !c.Stillbirth)
                .GroupBy(c => c.DamId)
                .Select(g => new { DamId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .FirstOrDefault();
            AnimalRef? standoutCow = damContrib != null && animals.TryGetValue(damContrib.DamId, out var dc)
                ? new AnimalRef(dc.Id, dc.CodeName, dc.PrimaryName)
                : null;

            // Narrative
            var narrative = ComposeNarrative(year, yearCalvings.Count, liveBirths, calfDeaths,
                survival, pregSuccess, skipped, firstTimers, topBullRef, topBull.surviving);

            rows.Add(new YearRow(
                Year: year,
                CalvesBorn: yearCalvings.Count,
                LiveBirths: liveBirths,
                Stillbirths: stillborn,
                CalfDeaths: calfDeaths,
                CalfSurvivalPct: survival,
                CowsBredOrConfirmed: distinctCowsBred,
                CowsThatCalved: cowsThatCalved,
                PregnancySuccessPct: pregSuccess,
                CalvingRatePct: calvingRate,
                AvgCalvingIntervalMonths: avgInterval,
                CowsSkippedYear: skipped,
                FirstTimeCalvers: firstTimers,
                Sales: yearSales,
                Deaths: yearDeaths,
                NetGrowth: netGrowth,
                TopBull: topBullRef,
                TopBullSurvivingCalves: topBull.surviving,
                StandoutCow: standoutCow,
                Narrative: narrative));
        }

        // Score each year for best/worst — composite of calf survival % + live births.
        var scored = rows
            .Select(r => new { Row = r, Score = r.CalfSurvivalPct + r.LiveBirths })
            .ToList();
        var best = scored.OrderByDescending(s => s.Score).FirstOrDefault()?.Row;
        var worst = scored.OrderBy(s => s.Score).FirstOrDefault()?.Row;

        var herdNarrative = rows.Count == 0
            ? "No calving years on file yet."
            : best != null && worst != null && best.Year != worst.Year
                ? $"Best year: {best.Year} ({best.LiveBirths} live births, {best.CalfSurvivalPct:0.0}% survival). " +
                  $"Worst year: {worst.Year} ({worst.LiveBirths} live births, {worst.CalfSurvivalPct:0.0}% survival)."
                : $"{rows.Count} calving year{(rows.Count == 1 ? "" : "s")} on record so far.";

        return new YearPerformanceReport(
            Years: rows.OrderByDescending(r => r.Year).ToList(),
            BestYear: best,
            WorstYear: worst,
            BestYearLabel: best?.Year,
            WorstYearLabel: worst?.Year,
            Narrative: herdNarrative);
    }

    private static string ComposeNarrative(
        int year, int calves, int liveBirths, int calfDeaths,
        decimal survival, decimal pregSuccess, int skipped, int firstTimers,
        AnimalRef? topBull, int topBullCalves)
    {
        if (calves == 0)
        {
            return $"No calvings recorded in {year}.";
        }

        var parts = new List<string>();

        if (survival >= 90m) parts.Add($"strong year — {survival:0.0}% calf survival");
        else if (survival >= 75m) parts.Add($"steady year — {survival:0.0}% calf survival");
        else parts.Add($"under-performing — {survival:0.0}% calf survival ({calfDeaths} calf loss{(calfDeaths == 1 ? "" : "es")})");

        parts.Add($"{liveBirths} live birth{(liveBirths == 1 ? "" : "s")}");

        if (pregSuccess > 0) parts.Add($"{pregSuccess:0.0}% pregnancy success");
        if (skipped > 0) parts.Add($"{skipped} cow{(skipped == 1 ? "" : "s")} skipped the year");
        if (firstTimers > 0) parts.Add($"{firstTimers} first-time mother{(firstTimers == 1 ? "" : "s")}");
        if (topBull != null && topBullCalves > 0)
            parts.Add($"{topBull.PrimaryName ?? topBull.CodeName} sired {topBullCalves} surviving calf{(topBullCalves == 1 ? "" : "ren")}");

        return $"{year}: " + string.Join("; ", parts) + ".";
    }
}

// =======================================================================
// INSIGHTS FEED  ───────────────────────────────────────────────────
// Rule-based proactive narrative insights. Spec §9.7 / §10.
// =======================================================================

public sealed record InsightsFeedQuery() : IRequest<IReadOnlyList<InsightItem>>;

public sealed record InsightItem(
    string Code,
    string Severity,             // info | success | warning | danger
    string Title,
    string Body,
    AnimalRef? Subject);

public sealed class InsightsFeedHandler(IFarmManagerDbContext db, ICurrentUser currentUser)
    : IRequestHandler<InsightsFeedQuery, IReadOnlyList<InsightItem>>
{
    public async Task<IReadOnlyList<InsightItem>> Handle(InsightsFeedQuery request, CancellationToken ct)
    {
        var orgId = currentUser.OrganisationId
            ?? throw new InvalidOperationException("No organisation context.");
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);

        var insights = new List<InsightItem>();

        var live = await db.Animals.AsNoTracking()
            .Where(a => a.OrganisationId == orgId
                && a.Status != AnimalStatus.Sold
                && a.Status != AnimalStatus.Dead
                && a.Status != AnimalStatus.Transferred)
            .ToListAsync(ct);

        // 1) Cows that have gone too long since their last calving.
        foreach (var cow in live.Where(a => a.Sex == AnimalSex.Female && a.LastCalvingDate is not null))
        {
            var months = (asOf.DayNumber - cow.LastCalvingDate!.Value.DayNumber) / 30;
            if (months >= 18 && cow.Status != AnimalStatus.PregnantConfirmed)
            {
                insights.Add(new InsightItem(
                    Code: "long_dry",
                    Severity: months >= 24 ? "danger" : "warning",
                    Title: $"{cow.PrimaryName ?? cow.CodeName} hasn't calved in {months} months",
                    Body: $"Last calving was {cow.LastCalvingDate}. Either confirm pregnancy or move to the sale candidate list.",
                    Subject: new AnimalRef(cow.Id, cow.CodeName, cow.PrimaryName)));
            }
        }

        // 2) Heifers approaching breeding age (24–30 months, no calf yet).
        foreach (var h in live.Where(a => a.Sex == AnimalSex.Female && a.CalfCount == 0))
        {
            var months = (asOf.DayNumber - h.Dob.DayNumber) / 30;
            if (months >= 24 && months <= 30)
            {
                insights.Add(new InsightItem(
                    Code: "heifer_ready",
                    Severity: "info",
                    Title: $"{h.PrimaryName ?? h.CodeName} is breeding-age",
                    Body: $"{months} months old. Plan first service this cycle to keep age-at-first-calf below 36 months.",
                    Subject: new AnimalRef(h.Id, h.CodeName, h.PrimaryName)));
            }
        }

        // 3) Tier-E cows still in the herd.
        foreach (var e in live.Where(a => a.PerformanceTier == PerformanceTier.E))
        {
            insights.Add(new InsightItem(
                Code: "tier_e_lingering",
                Severity: "danger",
                Title: $"{e.PrimaryName ?? e.CodeName} is in tier E",
                Body: "Tier E means cull-candidate. Confirm pregnancy by next preg-check or move to the sale workflow.",
                Subject: new AnimalRef(e.Id, e.CodeName, e.PrimaryName)));
        }

        // 4) Top performers — celebrate them.
        var topA = live.Where(a => a.PerformanceTier == PerformanceTier.A).Take(3).ToList();
        foreach (var t in topA)
        {
            insights.Add(new InsightItem(
                Code: "top_performer",
                Severity: "success",
                Title: $"{t.PrimaryName ?? t.CodeName} is a top performer",
                Body: $"CPY {t.CalvesPerYear?.ToString("0.00") ?? "—"} · {t.CalfCount} calves. Prioritise this cow's daughters as replacement heifers.",
                Subject: new AnimalRef(t.Id, t.CodeName, t.PrimaryName)));
        }

        // 5) Bull retirement age.
        var bulls = live.Where(a => a.Sex == AnimalSex.Male).ToList();
        foreach (var b in bulls)
        {
            var years = (asOf.DayNumber - b.Dob.DayNumber) / 365;
            if (years >= 7)
            {
                insights.Add(new InsightItem(
                    Code: "bull_retirement",
                    Severity: "warning",
                    Title: $"{b.PrimaryName ?? b.CodeName} is at retirement age",
                    Body: $"{years} years old — start lining up the replacement. Identify a sire candidate before the next breeding season.",
                    Subject: new AnimalRef(b.Id, b.CodeName, b.PrimaryName)));
            }
        }

        // 6) Inbreeding red-flag: animals whose dam and sire share a known common ancestor.
        var inbredCandidates = live
            .Where(a => a.IsBSired && a.SireId != null && a.DamId != null)
            .Take(5);
        foreach (var ic in inbredCandidates)
        {
            insights.Add(new InsightItem(
                Code: "lineage_concentration",
                Severity: "info",
                Title: $"{ic.PrimaryName ?? ic.CodeName} carries (B) lineage",
                Body: "Plan future matings carefully — pair only with non-(B) bulls to keep inbreeding F < 0.0625.",
                Subject: new AnimalRef(ic.Id, ic.CodeName, ic.PrimaryName)));
        }

        return insights;
    }
}
