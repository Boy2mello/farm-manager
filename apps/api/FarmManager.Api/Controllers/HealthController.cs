using FarmManager.Application.Common.Interfaces;
using FarmManager.Application.Health.Commands;
using FarmManager.Domain.Health;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmManager.Api.Controllers;

[ApiController]
[Route("api/v1/health")]
[Authorize]
public sealed class HealthController(ISender mediator, IFarmManagerDbContext db, ICurrentUser currentUser) : ControllerBase
{
    public sealed record RecordHealthRequest(
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
        string? Notes);

    [HttpPost("events")]
    public async Task<IActionResult> Record([FromBody] RecordHealthRequest body, CancellationToken ct)
    {
        var id = await mediator.Send(new RecordHealthEventCommand(
            body.AnimalId, body.EventType, body.EventDate, body.ProductId, body.ProductName,
            body.DoseAmount, body.DoseUnit, body.Route, body.VetUserId, body.BatchNumber,
            body.Expiry, body.WithdrawalDays, body.NextDueDays, body.Notes), ct);
        return Created($"/api/v1/health/events/{id}", new { id });
    }

    [HttpGet("due")]
    public async Task<IActionResult> Due([FromQuery] int withinDays = 30, CancellationToken ct = default)
    {
        var orgId = currentUser.OrganisationId
            ?? throw new InvalidOperationException("No organisation context.");
        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(withinDays);

        var rows = await db.Animals
            .AsNoTracking()
            .Where(a => a.OrganisationId == orgId)
            .SelectMany(a => db.Set<HealthEvent>()
                .Where(h => h.AnimalId == a.Id && h.NextDueDate != null && h.NextDueDate <= cutoff)
                .OrderByDescending(h => h.EventDate)
                .Take(1)
                .Select(h => new
                {
                    a.Id,
                    a.CodeName,
                    a.PrimaryName,
                    EventType = h.EventType,
                    NextDueDate = h.NextDueDate!.Value,
                    WithdrawalUntil = h.WithdrawalUntil,
                }))
            .ToListAsync(ct);

        return Ok(rows);
    }
}
