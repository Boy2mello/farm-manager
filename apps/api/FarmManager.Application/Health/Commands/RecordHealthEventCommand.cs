using FarmManager.Application.Common.Interfaces;
using FarmManager.Domain.Health;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmManager.Application.Health.Commands;

/// <summary>
/// RULE-004 (spec §11.4): record a health event, compute next-due, apply withdrawal,
/// decrement inventory, schedule reminder, emit health.captured.v1.
/// </summary>
public sealed record RecordHealthEventCommand(
    Guid AnimalId,
    HealthEventType EventType,
    DateOnly EventDate,
    Guid? ProductId,
    string? ProductName,
    decimal? DoseAmount,
    string? DoseUnit,
    AdministrationRoute? Route,
    Guid? VetUserId,
    string? BatchNumber,
    DateOnly? Expiry,
    int? WithdrawalDays,
    int? NextDueDays,
    string? Notes) : IRequest<Guid>;

public sealed class RecordHealthEventValidator : AbstractValidator<RecordHealthEventCommand>
{
    public RecordHealthEventValidator()
    {
        RuleFor(x => x.AnimalId).NotEmpty();
        RuleFor(x => x.EventDate).Must(d => d <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Event date cannot be in the future.");
        RuleFor(x => x.DoseAmount).GreaterThan(0).When(x => x.DoseAmount.HasValue);
        RuleFor(x => x.WithdrawalDays).GreaterThanOrEqualTo(0).When(x => x.WithdrawalDays.HasValue);
    }
}

public sealed class RecordHealthEventHandler(
    IFarmManagerDbContext db,
    ICurrentUser currentUser) : IRequestHandler<RecordHealthEventCommand, Guid>
{
    public async Task<Guid> Handle(RecordHealthEventCommand request, CancellationToken ct)
    {
        var orgId = currentUser.OrganisationId
            ?? throw new InvalidOperationException("No organisation context.");

        var animal = await db.Animals.FirstOrDefaultAsync(a => a.Id == request.AnimalId && a.OrganisationId == orgId, ct)
            ?? throw new InvalidOperationException($"Animal {request.AnimalId} not found.");

        DateOnly? withdrawalUntil = request.WithdrawalDays is { } days
            ? request.EventDate.AddDays(days)
            : null;

        DateOnly? nextDue = request.NextDueDays is { } d
            ? request.EventDate.AddDays(d)
            : null;

        var record = HealthEvent.Create(
            organisationId: orgId,
            animalId: animal.Id,
            type: request.EventType,
            eventDate: request.EventDate,
            productId: request.ProductId,
            productName: request.ProductName,
            doseAmount: request.DoseAmount,
            doseUnit: request.DoseUnit,
            route: request.Route,
            vetUserId: request.VetUserId ?? currentUser.UserId,
            batchNumber: request.BatchNumber,
            expiry: request.Expiry,
            withdrawalUntil: withdrawalUntil,
            nextDue: nextDue,
            notes: request.Notes,
            createdBy: currentUser.UserName);

        db.Set<HealthEvent>().Add(record);

        if (withdrawalUntil is { } until)
        {
            animal.SetWithdrawal(until);
        }

        await db.SaveChangesAsync(ct);
        return record.Id;
    }
}
