using FarmManager.Application.Common.Interfaces;
using FarmManager.Domain.Animals;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmManager.Application.Animals.Commands.RegisterAnimal;

public sealed record RegisterAnimalCommand(
    string? PrimaryName,
    AnimalSex Sex,
    DateOnly Dob,
    DobPrecision DobPrecision,
    AnimalSource Source,
    Guid? DamId = null,
    Guid? SireId = null,
    Guid? BreedId = null,
    Guid? FarmId = null,
    IReadOnlyList<string>? Aliases = null) : IRequest<RegisterAnimalResult>;

public sealed record RegisterAnimalResult(Guid AnimalId, string CodeName);

public sealed class RegisterAnimalValidator : AbstractValidator<RegisterAnimalCommand>
{
    public RegisterAnimalValidator()
    {
        RuleFor(x => x.Dob)
            .Must(d => d <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("DOB cannot be in the future.");

        RuleFor(x => x.PrimaryName)
            .MaximumLength(120);
    }
}

public sealed class RegisterAnimalHandler(
    IFarmManagerDbContext db,
    ICodeNameGenerator codeNames,
    ICurrentUser currentUser) : IRequestHandler<RegisterAnimalCommand, RegisterAnimalResult>
{
    public async Task<RegisterAnimalResult> Handle(RegisterAnimalCommand request, CancellationToken ct)
    {
        var organisationId = currentUser.OrganisationId
            ?? throw new InvalidOperationException("Current user has no organisation context.");

        var code = await codeNames.NextAsync(organisationId, request.Source, request.Dob.Year, ct);

        var animal = Animal.Register(
            organisationId: organisationId,
            codeName: code,
            sex: request.Sex,
            dob: request.Dob,
            dobPrecision: request.DobPrecision,
            source: request.Source,
            damId: request.DamId,
            sireId: request.SireId,
            breedId: request.BreedId,
            farmId: request.FarmId,
            primaryName: request.PrimaryName,
            aliases: request.Aliases);

        db.Animals.Add(animal);
        await db.SaveChangesAsync(ct);

        return new RegisterAnimalResult(animal.Id, animal.CodeName);
    }
}
