using System.Text.Json;
using FarmManager.Application.Animals;
using FarmManager.Application.Common.Exceptions;
using FarmManager.Application.Common.Interfaces;
using FarmManager.Domain.Animals;
using FarmManager.Domain.Breeding;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmManager.Application.Calvings.Commands.RecordCalving;

/// <summary>
/// RULE-001 (Calving Event Recorded) + RULE-019 (Auto Code-Name Generation), spec §11.1 + §11.19.
/// </summary>
public sealed record RecordCalvingCommand(
    Guid DamId,
    DateOnly CalvingDate,
    AnimalSex CalfSex,
    Guid? SireId,
    string? SireExternalNote,
    int DifficultyScore,
    bool AssistanceRequired,
    bool PlacentaDelivered,
    int? MotheringAbility,
    bool Stillbirth,
    decimal? CalfWeightKg,
    int? CalfVigour,
    string? Notes) : IRequest<RecordCalvingResult>;

public sealed record RecordCalvingResult(Guid CalvingEventId, Guid CalfId, string CalfCodeName);

public sealed class RecordCalvingValidator : AbstractValidator<RecordCalvingCommand>
{
    public RecordCalvingValidator()
    {
        RuleFor(x => x.DamId).NotEmpty();
        RuleFor(x => x.CalvingDate)
            .Must(d => d <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Calving date cannot be in the future.");
        RuleFor(x => x.DifficultyScore).InclusiveBetween(1, 5);
        RuleFor(x => x.MotheringAbility).InclusiveBetween(1, 5).When(x => x.MotheringAbility.HasValue);
        RuleFor(x => x.CalfVigour).InclusiveBetween(1, 5).When(x => x.CalfVigour.HasValue);
        RuleFor(x => x.CalfWeightKg).GreaterThan(0).When(x => x.CalfWeightKg.HasValue);
    }
}

public sealed class RecordCalvingHandler(
    IFarmManagerDbContext db,
    ICodeNameGenerator codeNames,
    ICurrentUser currentUser,
    IDomainEventDispatcher events) : IRequestHandler<RecordCalvingCommand, RecordCalvingResult>
{
    /// <summary>
    /// Boshomane's record id is resolved at runtime via the seed-data lookup. We compare against
    /// the configured ResidentBullCode in <see cref="LineageConstants"/>.
    /// </summary>
    public async Task<RecordCalvingResult> Handle(RecordCalvingCommand request, CancellationToken ct)
    {
        var organisationId = currentUser.OrganisationId
            ?? throw new InvalidOperationException("Current user has no organisation context.");

        // ----- Preconditions per spec §11.1 -----
        var dam = await db.Animals.FirstOrDefaultAsync(a => a.Id == request.DamId && a.OrganisationId == organisationId, ct)
            ?? throw new InvalidOperationException($"Dam {request.DamId} not found.");

        if (dam.Sex != AnimalSex.Female)
        {
            throw new InvalidOperationException("Dam must be female.");
        }

        var ageInMonths = (request.CalvingDate.DayNumber - dam.Dob.DayNumber) / 30;
        if (ageInMonths < 18)
        {
            throw new InvalidOperationException("Dam must be at least 18 months old at calving.");
        }

        if (dam.LastCalvingDate is { } prev && request.CalvingDate.DayNumber - prev.DayNumber < 250)
        {
            throw new InvalidOperationException(
                $"Calving date is too close to previous calving ({prev:yyyy-MM-dd}). Minimum gap is 250 days.");
        }

        // Spec §7.3 conflict rule: two calvings same dam within 24 h → manager intervention.
        var conflictWindowStart = request.CalvingDate.AddDays(-1);
        var conflictWindowEnd = request.CalvingDate.AddDays(1);
        var hasNearbyCalving = await db.CalvingEvents
            .AsNoTracking()
            .AnyAsync(c => c.DamId == dam.Id
                && c.CalvingDate >= conflictWindowStart
                && c.CalvingDate <= conflictWindowEnd, ct);

        if (hasNearbyCalving)
        {
            throw new ConflictException(
                "calving_too_close",
                $"A calving was already recorded for this dam within 24 hours of {request.CalvingDate:yyyy-MM-dd}. Manager review required.");
        }

        // ----- 1. Assign code-name (RULE-019) -----
        var calfCode = await codeNames.NextAsync(organisationId, AnimalSource.BornOnFarm, request.CalvingDate.Year, ct);

        // ----- 2. Create calf -----
        var calf = Animal.Register(
            organisationId: organisationId,
            codeName: calfCode,
            sex: request.CalfSex,
            dob: request.CalvingDate,
            dobPrecision: DobPrecision.Day,
            source: AnimalSource.BornOnFarm,
            damId: dam.Id,
            sireId: request.SireId,
            farmId: dam.FarmId);

        if (request.Stillbirth)
        {
            calf.TransitionTo(AnimalStatus.Dead);
        }

        // RULE-011: (B)-sired marking happens at the Infrastructure layer using the resident bull's id.

        db.Animals.Add(calf);

        // ----- 3. Persist calving event -----
        var calving = CalvingEvent.Create(
            organisationId: organisationId,
            damId: dam.Id,
            calfId: calf.Id,
            calvingDate: request.CalvingDate,
            sireId: request.SireId,
            sireExternalNote: request.SireExternalNote,
            difficultyScore: request.DifficultyScore,
            assistanceRequired: request.AssistanceRequired,
            placentaDelivered: request.PlacentaDelivered,
            motheringAbility: request.MotheringAbility,
            stillbirth: request.Stillbirth,
            calfWeightKg: request.CalfWeightKg,
            calfVigour: request.CalfVigour,
            notes: request.Notes,
            createdBy: currentUser.UserName);

        db.CalvingEvents.Add(calving);

        // ----- 4. Update dam state -----
        dam.TransitionTo(AnimalStatus.Lactating);
        dam.SetLastCalving(request.CalvingDate);

        // Recompute denormalised counts. Full performance recalc is RULE-007 (TierEvaluator).
        var totalCalves = await db.CalvingEvents.CountAsync(c => c.DamId == dam.Id, ct) + 1;
        var aliveCalves = await db.Animals.CountAsync(
            a => a.DamId == dam.Id && a.Status != AnimalStatus.Dead, ct);
        if (!request.Stillbirth) aliveCalves++;

        dam.UpdatePerformance(totalCalves, aliveCalves, dam.AvgCalvingIntervalDays, dam.CalvesPerYear, dam.PerformanceTier);

        // ----- 5. Save & dispatch events -----
        await db.SaveChangesAsync(ct);

        await events.DispatchAsync(new[]
        {
            new CalvingRecordedEvent(
                CalvingEventId: calving.Id,
                OrganisationId: organisationId,
                DamId: dam.Id,
                CalfId: calf.Id,
                CalfCodeName: calf.CodeName,
                Stillbirth: request.Stillbirth,
                OccurredAt: DateTimeOffset.UtcNow),
        }, ct);

        return new RecordCalvingResult(calving.Id, calf.Id, calf.CodeName);
    }
}

public static class LineageConstants
{
    /// <summary>
    /// The given name of the resident bull. RULE-011 marks every calf sired by him as (B).
    /// Configured in seed data; matched by primary name OR code-name as the Application layer cannot
    /// hard-code an id (it is generated at seed time).
    /// </summary>
    public const string ResidentBullName = "Boshomane";

    /// <summary>
    /// Spec §12.3 inbreeding coefficient thresholds.
    /// </summary>
    public const decimal WarnThreshold = 0.0625m;   // First cousins
    public const decimal OverrideThreshold = 0.1250m; // Half-siblings
    public const decimal HardBlockThreshold = 0.2500m; // Full siblings / parent-offspring
}
