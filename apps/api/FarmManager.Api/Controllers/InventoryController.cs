using FarmManager.Application.Common.Interfaces;
using FarmManager.Application.Inventory.Commands;
using FarmManager.Domain.Inventory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmManager.Api.Controllers;

[ApiController]
[Route("api/v1/inventory")]
[Authorize]
public sealed class InventoryController(ISender mediator, IFarmManagerDbContext db, ICurrentUser currentUser) : ControllerBase
{
    public sealed record CreateItemRequest(
        string Sku, string Name, InventoryCategory Category, string Unit,
        decimal? ReorderPoint, decimal? CostPerUnit, bool ColdChain);

    [HttpGet("items")]
    public async Task<IActionResult> Items(CancellationToken ct)
    {
        var orgId = currentUser.OrganisationId
            ?? throw new InvalidOperationException("No organisation context.");
        var items = await db.InventoryItems.AsNoTracking()
            .Where(i => i.OrganisationId == orgId)
            .OrderBy(i => i.Name)
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost("items")]
    public async Task<IActionResult> Create([FromBody] CreateItemRequest body, CancellationToken ct)
    {
        var id = await mediator.Send(new CreateInventoryItemCommand(
            body.Sku, body.Name, body.Category, body.Unit, body.ReorderPoint, body.CostPerUnit, body.ColdChain), ct);
        return Created($"/api/v1/inventory/items/{id}", new { id });
    }

    public sealed record StockMovementRequest(
        Guid ItemId, StockMovementKind Kind, decimal Quantity, DateOnly MovementDate,
        string? BatchNumber, DateOnly? Expiry, Guid? RelatedEventId, string? Notes);

    [HttpPost("movements")]
    public async Task<IActionResult> Move([FromBody] StockMovementRequest body, CancellationToken ct)
    {
        var id = await mediator.Send(new RecordStockMovementCommand(
            body.ItemId, body.Kind, body.Quantity, body.MovementDate, body.BatchNumber,
            body.Expiry, body.RelatedEventId, body.Notes), ct);
        return Created($"/api/v1/inventory/movements/{id}", new { id });
    }

    [HttpGet("expiring")]
    public async Task<IActionResult> Expiring([FromQuery] int withinDays = 30, CancellationToken ct = default)
    {
        var orgId = currentUser.OrganisationId
            ?? throw new InvalidOperationException("No organisation context.");
        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(withinDays);

        var rows = await db.StockMovements.AsNoTracking()
            .Where(m => m.OrganisationId == orgId && m.Expiry != null && m.Expiry <= cutoff)
            .OrderBy(m => m.Expiry)
            .ToListAsync(ct);
        return Ok(rows);
    }
}
