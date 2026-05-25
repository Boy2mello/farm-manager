using FarmManager.Application.Common.Exceptions;
using FarmManager.Application.Common.Interfaces;
using FarmManager.Domain.Animals;
using FarmManager.Domain.Commerce;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmManager.Application.Commerce.Commands;

public sealed record RecordSaleCommand(
    Guid AnimalId,
    DateOnly SaleDate,
    string Buyer,
    decimal PriceTotal,
    decimal? PricePerKg,
    decimal? WeightKg,
    decimal Commission,
    decimal Transport,
    string? PaperworkReference,
    string? Notes) : IRequest<Guid>;

public sealed class RecordSaleValidator : AbstractValidator<RecordSaleCommand>
{
    public RecordSaleValidator()
    {
        RuleFor(x => x.AnimalId).NotEmpty();
        RuleFor(x => x.Buyer).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PriceTotal).GreaterThan(0);
        RuleFor(x => x.SaleDate).Must(d => d <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Sale date cannot be in the future.");
    }
}

/// <summary>
/// RULE-006 (spec §11.6) + RULE-012 (spec §11.12): record the sale, refuse if the animal is still
/// within a treatment withdrawal period, mark the animal Sold, emit sale.completed.v1. Sale double
/// is prevented by the unique index on animal_id (spec §7.3 conflict rule).
/// </summary>
public sealed class RecordSaleHandler(
    IFarmManagerDbContext db,
    ICurrentUser currentUser) : IRequestHandler<RecordSaleCommand, Guid>
{
    public async Task<Guid> Handle(RecordSaleCommand request, CancellationToken ct)
    {
        var orgId = currentUser.OrganisationId
            ?? throw new InvalidOperationException("No organisation context.");

        var animal = await db.Animals
            .FirstOrDefaultAsync(a => a.Id == request.AnimalId && a.OrganisationId == orgId, ct)
            ?? throw new InvalidOperationException($"Animal {request.AnimalId} not found.");

        if (animal.Status == AnimalStatus.Sold)
        {
            throw new ConflictException("animal_already_sold",
                $"Animal {animal.CodeName} is already sold.");
        }

        if (animal.WithdrawalUntil is { } until && until >= request.SaleDate)
        {
            throw new ConflictException("withdrawal_active",
                $"Sale blocked: withdrawal period ends {until:yyyy-MM-dd}.");
        }

        var sale = SaleEvent.Create(
            organisationId: orgId,
            animalId: animal.Id,
            saleDate: request.SaleDate,
            buyer: request.Buyer,
            priceTotal: request.PriceTotal,
            pricePerKg: request.PricePerKg,
            weightKg: request.WeightKg,
            commission: request.Commission,
            transport: request.Transport,
            paperworkReference: request.PaperworkReference,
            notes: request.Notes,
            createdBy: currentUser.UserName);

        db.SaleEvents.Add(sale);
        animal.TransitionTo(AnimalStatus.Sold);

        await db.SaveChangesAsync(ct);
        return sale.Id;
    }
}
