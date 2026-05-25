using System.Text.Json;
using FarmManager.Application.Common.Events;
using FarmManager.Application.Common.Interfaces;
using FarmManager.Application.Flagging;
using FarmManager.Domain.Animals;
using FarmManager.Domain.Common;
using FarmManager.Domain.Flagging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmManager.Application.Analytics.Jobs;

/// <summary>
/// RULE-007 + RULE-017 (spec §11.7 / §11.17). Runs nightly, re-tiers every breeding cow, and
/// reconciles the flag-set, emitting tier.changed / flag.assigned / flag.resolved domain events.
/// </summary>
public sealed class NightlyTierRecalcJob(
    IFarmManagerDbContext db,
    IMetricsCalculator metrics,
    IDomainEventDispatcher dispatcher,
    ILogger<NightlyTierRecalcJob> logger)
{
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);

        // Track every breeding cow (female, not dead/sold/transferred).
        var cowIds = await db.Animals
            .AsNoTracking()
            .Where(a => a.Sex == AnimalSex.Female
                && a.Status != AnimalStatus.Dead
                && a.Status != AnimalStatus.Sold
                && a.Status != AnimalStatus.Transferred)
            .Select(a => a.Id)
            .ToListAsync(ct);

        logger.LogInformation("NightlyTierRecalc starting for {Count} cows", cowIds.Count);

        var raised = new List<IDomainEvent>();
        var now = DateTimeOffset.UtcNow;

        foreach (var cowId in cowIds)
        {
            var m = await metrics.ComputeForCowAsync(cowId, asOf, ct);
            if (m is null) continue;

            var tier = TierEvaluator.Evaluate(m);
            var animal = await db.Animals.FirstAsync(a => a.Id == cowId, ct);
            var previous = animal.PerformanceTier;

            if (tier.Tier != previous)
            {
                animal.UpdatePerformance(animal.CalfCount, animal.CalvesAlive, animal.AvgCalvingIntervalDays, animal.CalvesPerYear, tier.Tier);
                db.TierAssignments.Add(TierAssignment.Record(
                    animal.OrganisationId, animal.Id, previous, tier.Tier, tier.Reason, tier.MetricsJson));

                raised.Add(new TierChangedEvent(
                    animal.OrganisationId, animal.Id, previous, tier.Tier, tier.Reason, now));
            }

            // Flag reconciliation — additive: new flags inserted; flags no longer applicable resolved.
            var desired = CowFlagCatalogue.Evaluate(m).ToDictionary(f => f.Code);
            var open = await db.Flags
                .Where(f => f.AnimalId == cowId && f.ResolvedAt == null)
                .ToListAsync(ct);

            foreach (var f in open.Where(f => !desired.ContainsKey(f.Code)))
            {
                f.Resolve();
                raised.Add(new FlagResolvedEvent(animal.OrganisationId, animal.Id, f.Code, now));
            }

            foreach (var (code, assessment) in desired)
            {
                if (open.Any(o => o.Code == code)) continue;
                db.Flags.Add(Flag.Open(animal.OrganisationId, animal.Id, code, assessment.Severity, assessment.Reason, assessment.MetricsJson));
                raised.Add(new FlagAssignedEvent(animal.OrganisationId, animal.Id, code, assessment.Severity, assessment.Reason, now));
            }
        }

        await db.SaveChangesAsync(ct);
        await dispatcher.DispatchAsync(raised, ct);

        logger.LogInformation("NightlyTierRecalc completed: {Events} events emitted", raised.Count);
    }
}

public sealed record TierChangedEvent(
    Guid OrganisationId,
    Guid AnimalId,
    PerformanceTier Previous,
    PerformanceTier Current,
    string Reason,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record FlagAssignedEvent(
    Guid OrganisationId,
    Guid AnimalId,
    string FlagCode,
    FlagSeverity Severity,
    string Reason,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record FlagResolvedEvent(
    Guid OrganisationId,
    Guid AnimalId,
    string FlagCode,
    DateTimeOffset OccurredAt) : IDomainEvent;
