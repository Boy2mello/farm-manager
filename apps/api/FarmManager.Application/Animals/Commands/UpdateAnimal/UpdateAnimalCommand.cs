using FarmManager.Application.Common.Interfaces;
using FarmManager.Domain.Animals;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmManager.Application.Animals.Commands.UpdateAnimal;

/// <summary>
/// Mutates the editable parts of an Animal — status, name, aliases, withdrawal date.
/// The code-name is immutable (spec §8.1.1). Lineage is immutable through this command
/// (use the calving / mating commands instead).
/// </summary>
public sealed record UpdateAnimalCommand(
    Guid AnimalId,
    AnimalStatus? Status = null,
    string? PrimaryName = null,
    IReadOnlyList<string>? Aliases = null,
    DateOnly? WithdrawalUntil = null,
    DateOnly? DisposalDate = null,
    string? DisposalReason = null) : IRequest<Unit>;

public sealed class UpdateAnimalValidator : AbstractValidator<UpdateAnimalCommand>
{
    public UpdateAnimalValidator()
    {
        RuleFor(x => x.AnimalId).NotEmpty();
        RuleFor(x => x.PrimaryName)
            .MaximumLength(120)
            .When(x => !string.IsNullOrWhiteSpace(x.PrimaryName));
    }
}

public sealed class UpdateAnimalHandler(
    IFarmManagerDbContext db,
    ICurrentUser currentUser) : IRequestHandler<UpdateAnimalCommand, Unit>
{
    public async Task<Unit> Handle(UpdateAnimalCommand request, CancellationToken ct)
    {
        var orgId = currentUser.OrganisationId
            ?? throw new InvalidOperationException("No organisation context.");

        var animal = await db.Animals
            .FirstOrDefaultAsync(a => a.Id == request.AnimalId && a.OrganisationId == orgId, ct)
            ?? throw new InvalidOperationException($"Animal {request.AnimalId} not found.");

        if (request.Status is { } status && status != animal.Status)
        {
            animal.TransitionTo(status);
        }

        if (request.PrimaryName != null || request.Aliases != null)
        {
            animal.Rename(
                request.PrimaryName ?? animal.PrimaryName,
                request.Aliases ?? animal.Aliases);
        }

        if (request.WithdrawalUntil is { } withdrawal)
        {
            animal.SetWithdrawal(withdrawal);
        }

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
