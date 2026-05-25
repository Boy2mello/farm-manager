using FarmManager.Application.Common.Interfaces;
using FarmManager.Domain.Inventory;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmManager.Application.Inventory.Commands;

public sealed record CreateInventoryItemCommand(
    string Sku,
    string Name,
    InventoryCategory Category,
    string Unit,
    decimal? ReorderPoint,
    decimal? CostPerUnit,
    bool ColdChain) : IRequest<Guid>;

public sealed class CreateInventoryItemValidator : AbstractValidator<CreateInventoryItemCommand>
{
    public CreateInventoryItemValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(16);
        RuleFor(x => x.ReorderPoint).GreaterThanOrEqualTo(0).When(x => x.ReorderPoint.HasValue);
    }
}

public sealed class CreateInventoryItemHandler(
    IFarmManagerDbContext db,
    ICurrentUser currentUser) : IRequestHandler<CreateInventoryItemCommand, Guid>
{
    public async Task<Guid> Handle(CreateInventoryItemCommand request, CancellationToken ct)
    {
        var orgId = currentUser.OrganisationId
            ?? throw new InvalidOperationException("No organisation context.");

        var item = InventoryItem.Create(orgId, request.Sku, request.Name, request.Category,
            request.Unit, request.ReorderPoint, request.CostPerUnit, request.ColdChain);

        db.InventoryItems.Add(item);
        await db.SaveChangesAsync(ct);
        return item.Id;
    }
}

public sealed record RecordStockMovementCommand(
    Guid ItemId,
    StockMovementKind Kind,
    decimal Quantity,
    DateOnly MovementDate,
    string? BatchNumber,
    DateOnly? Expiry,
    Guid? RelatedEventId,
    string? Notes) : IRequest<Guid>;

public sealed class RecordStockMovementValidator : AbstractValidator<RecordStockMovementCommand>
{
    public RecordStockMovementValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public sealed class RecordStockMovementHandler(
    IFarmManagerDbContext db,
    ICurrentUser currentUser) : IRequestHandler<RecordStockMovementCommand, Guid>
{
    public async Task<Guid> Handle(RecordStockMovementCommand request, CancellationToken ct)
    {
        var orgId = currentUser.OrganisationId
            ?? throw new InvalidOperationException("No organisation context.");

        var item = await db.InventoryItems.FirstOrDefaultAsync(i => i.Id == request.ItemId && i.OrganisationId == orgId, ct)
            ?? throw new InvalidOperationException($"Inventory item {request.ItemId} not found.");

        var movement = StockMovement.Create(orgId, item.Id, request.Kind, request.Quantity,
            request.MovementDate, request.BatchNumber, request.Expiry, request.RelatedEventId,
            request.Notes, currentUser.UserName);

        db.StockMovements.Add(movement);
        item.Apply(movement.SignedDelta());

        await db.SaveChangesAsync(ct);
        return movement.Id;
    }
}
