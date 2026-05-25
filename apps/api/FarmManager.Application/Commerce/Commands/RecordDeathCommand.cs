using FarmManager.Application.Common.Exceptions;
using FarmManager.Application.Common.Interfaces;
using FarmManager.Domain.Animals;
using FarmManager.Domain.Commerce;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmManager.Application.Commerce.Commands;

public sealed record RecordDeathCommand(
    Guid AnimalId,
    DateOnly DeathDate,
    DeathCause Cause,
    string? SuspectedDisease,
    string? PostMortemFindings,
    bool InsuranceClaimable,
    string? Notes) : IRequest<Guid>;

public sealed class RecordDeathValidator : AbstractValidator<RecordDeathCommand>
{
    public RecordDeathValidator()
    {
        RuleFor(x => x.AnimalId).NotEmpty();
        RuleFor(x => x.DeathDate).Must(d => d <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Death date cannot be in the future.");
    }
}

public sealed class RecordDeathHandler(
    IFarmManagerDbContext db,
    ICurrentUser currentUser) : IRequestHandler<RecordDeathCommand, Guid>
{
    public async Task<Guid> Handle(RecordDeathCommand request, CancellationToken ct)
    {
        var orgId = currentUser.OrganisationId
            ?? throw new InvalidOperationException("No organisation context.");

        var animal = await db.Animals
            .FirstOrDefaultAsync(a => a.Id == request.AnimalId && a.OrganisationId == orgId, ct)
            ?? throw new InvalidOperationException($"Animal {request.AnimalId} not found.");

        if (animal.Status == AnimalStatus.Dead)
        {
            throw new ConflictException("animal_already_dead",
                $"Animal {animal.CodeName} is already recorded dead.");
        }

        var death = DeathEvent.Create(
            organisationId: orgId,
            animalId: animal.Id,
            deathDate: request.DeathDate,
            cause: request.Cause,
            suspectedDisease: request.SuspectedDisease,
            postMortemFindings: request.PostMortemFindings,
            insuranceClaimable: request.InsuranceClaimable,
            notes: request.Notes,
            createdBy: currentUser.UserName);

        db.DeathEvents.Add(death);
        animal.TransitionTo(AnimalStatus.Dead);

        await db.SaveChangesAsync(ct);
        return death.Id;
    }
}
