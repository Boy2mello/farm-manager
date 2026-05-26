using FarmManager.Application.Common.Interfaces;
using FarmManager.Domain.Animals;
using FarmManager.Domain.Flagging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmManager.Application.Reporting.Queries;

// ---------- Herd Census ----------

public sealed record HerdCensusRow(
    Guid AnimalId,
    string CodeName,
    string? PrimaryName,
    AnimalSex Sex,
    DateOnly Dob,
    AnimalStatus Status,
    PerformanceTier PerformanceTier,
    bool IsBSired,
    string? DamName,
    string? SireName);

public sealed record HerdCensusReport(
    DateOnly AsOfDate,
    int TotalLive,
    int Females,
    int Males,
    int BSired,
    IReadOnlyList<HerdCensusRow> Rows);

public sealed record HerdCensusReportQuery() : IRequest<HerdCensusReport>;

public sealed class HerdCensusReportHandler(IFarmManagerDbContext db, ICurrentUser currentUser)
    : IRequestHandler<HerdCensusReportQuery, HerdCensusReport>
{
    public async Task<HerdCensusReport> Handle(HerdCensusReportQuery request, CancellationToken ct)
    {
        var orgId = currentUser.OrganisationId
            ?? throw new InvalidOperationException("No organisation context.");

        var animals = await db.Animals.AsNoTracking()
            .Where(a => a.OrganisationId == orgId
                && a.Status != AnimalStatus.Sold
                && a.Status != AnimalStatus.Dead
                && a.Status != AnimalStatus.Transferred)
            .OrderBy(a => a.CodeName)
            .ToListAsync(ct);

        var parentLookup = await db.Animals.AsNoTracking()
            .Where(a => a.OrganisationId == orgId)
            .Select(a => new { a.Id, a.PrimaryName, a.CodeName })
            .ToDictionaryAsync(a => a.Id, ct);

        string? NameOf(Guid? id) => id is { } gid && parentLookup.TryGetValue(gid, out var p)
            ? p.PrimaryName ?? p.CodeName
            : null;

        var rows = animals
            .Select(a => new HerdCensusRow(
                a.Id, a.CodeName, a.PrimaryName, a.Sex, a.Dob, a.Status,
                a.PerformanceTier, a.IsBSired, NameOf(a.DamId), NameOf(a.SireId)))
            .ToList();

        return new HerdCensusReport(
            AsOfDate: DateOnly.FromDateTime(DateTime.UtcNow),
            TotalLive: rows.Count,
            Females: rows.Count(r => r.Sex == AnimalSex.Female),
            Males: rows.Count(r => r.Sex == AnimalSex.Male),
            BSired: rows.Count(r => r.IsBSired),
            Rows: rows);
    }
}

// ---------- Performance Ranking ----------

public sealed record PerformanceRankingRow(
    Guid AnimalId,
    string CodeName,
    string? PrimaryName,
    DateOnly Dob,
    decimal AgeYears,
    int CalfCount,
    int CalvesAlive,
    decimal? CalvesPerYear,
    decimal? AvgCalvingIntervalDays,
    DateOnly? LastCalvingDate,
    AnimalStatus Status,
    PerformanceTier Tier,
    string? Reason);

public sealed record PerformanceRankingReport(
    DateOnly AsOfDate,
    IReadOnlyDictionary<string, int> TierCounts,
    IReadOnlyList<PerformanceRankingRow> Rows);

public sealed record PerformanceRankingReportQuery() : IRequest<PerformanceRankingReport>;

public sealed class PerformanceRankingReportHandler(IFarmManagerDbContext db, ICurrentUser currentUser)
    : IRequestHandler<PerformanceRankingReportQuery, PerformanceRankingReport>
{
    public async Task<PerformanceRankingReport> Handle(PerformanceRankingReportQuery request, CancellationToken ct)
    {
        var orgId = currentUser.OrganisationId
            ?? throw new InvalidOperationException("No organisation context.");

        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);

        var cows = await db.Animals.AsNoTracking()
            .Where(a => a.OrganisationId == orgId
                && a.Sex == AnimalSex.Female
                && a.Status != AnimalStatus.Dead
                && a.Status != AnimalStatus.Sold
                && a.Status != AnimalStatus.Transferred)
            .OrderBy(a => a.PerformanceTier == PerformanceTier.None ? 99 : (int)a.PerformanceTier)
            .ThenBy(a => a.CodeName)
            .ToListAsync(ct);

        var latestTierByAnimal = await db.TierAssignments.AsNoTracking()
            .Where(t => t.OrganisationId == orgId)
            .GroupBy(t => t.AnimalId)
            .Select(g => g.OrderByDescending(x => x.AssignedAt).First())
            .ToDictionaryAsync(t => t.AnimalId, t => t.Reason, ct);

        var rows = cows.Select(a => new PerformanceRankingRow(
                AnimalId: a.Id,
                CodeName: a.CodeName,
                PrimaryName: a.PrimaryName,
                Dob: a.Dob,
                AgeYears: Math.Round((decimal)(asOf.DayNumber - a.Dob.DayNumber) / 365.25m, 1),
                CalfCount: a.CalfCount,
                CalvesAlive: a.CalvesAlive,
                CalvesPerYear: a.CalvesPerYear,
                AvgCalvingIntervalDays: a.AvgCalvingIntervalDays,
                LastCalvingDate: a.LastCalvingDate,
                Status: a.Status,
                Tier: a.PerformanceTier,
                Reason: latestTierByAnimal.TryGetValue(a.Id, out var r) ? r : null))
            .ToList();

        var tierCounts = rows
            .GroupBy(r => r.Tier.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        return new PerformanceRankingReport(asOf, tierCounts, rows);
    }
}

// ---------- Cull Candidates ----------

public sealed record CullCandidateRow(
    Guid AnimalId,
    string CodeName,
    string? PrimaryName,
    DateOnly Dob,
    decimal AgeYears,
    int CalfCount,
    int CalvesAlive,
    decimal? CalvesPerYear,
    AnimalStatus Status,
    string? Reason,
    IReadOnlyList<string> ActiveFlags);

public sealed record CullCandidatesReport(
    DateOnly AsOfDate,
    int Count,
    IReadOnlyList<CullCandidateRow> Rows);

public sealed record CullCandidatesReportQuery() : IRequest<CullCandidatesReport>;

public sealed class CullCandidatesReportHandler(IFarmManagerDbContext db, ICurrentUser currentUser)
    : IRequestHandler<CullCandidatesReportQuery, CullCandidatesReport>
{
    public async Task<CullCandidatesReport> Handle(CullCandidatesReportQuery request, CancellationToken ct)
    {
        var orgId = currentUser.OrganisationId
            ?? throw new InvalidOperationException("No organisation context.");
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);

        var cull = await db.Animals.AsNoTracking()
            .Where(a => a.OrganisationId == orgId
                && a.PerformanceTier == PerformanceTier.E
                && a.Status != AnimalStatus.Dead
                && a.Status != AnimalStatus.Sold)
            .OrderBy(a => a.CodeName)
            .ToListAsync(ct);

        var ids = cull.Select(a => a.Id).ToHashSet();

        var openFlags = await db.Flags.AsNoTracking()
            .Where(f => ids.Contains(f.AnimalId) && f.ResolvedAt == null)
            .Select(f => new { f.AnimalId, f.Code })
            .ToListAsync(ct);

        var flagsByAnimal = openFlags
            .GroupBy(f => f.AnimalId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Code).ToList());

        var latestTierByAnimal = await db.TierAssignments.AsNoTracking()
            .Where(t => ids.Contains(t.AnimalId))
            .GroupBy(t => t.AnimalId)
            .Select(g => g.OrderByDescending(x => x.AssignedAt).First())
            .ToDictionaryAsync(t => t.AnimalId, t => t.Reason, ct);

        var rows = cull.Select(a => new CullCandidateRow(
                AnimalId: a.Id,
                CodeName: a.CodeName,
                PrimaryName: a.PrimaryName,
                Dob: a.Dob,
                AgeYears: Math.Round((decimal)(asOf.DayNumber - a.Dob.DayNumber) / 365.25m, 1),
                CalfCount: a.CalfCount,
                CalvesAlive: a.CalvesAlive,
                CalvesPerYear: a.CalvesPerYear,
                Status: a.Status,
                Reason: latestTierByAnimal.TryGetValue(a.Id, out var r) ? r : null,
                ActiveFlags: flagsByAnimal.TryGetValue(a.Id, out var fs) ? fs : new List<string>()))
            .ToList();

        return new CullCandidatesReport(asOf, rows.Count, rows);
    }
}
