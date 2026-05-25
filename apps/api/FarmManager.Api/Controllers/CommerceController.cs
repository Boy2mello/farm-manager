using FarmManager.Application.Commerce.Commands;
using FarmManager.Domain.Commerce;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManager.Api.Controllers;

[ApiController]
[Route("api/v1/commerce")]
[Authorize]
public sealed class CommerceController(ISender mediator) : ControllerBase
{
    public sealed record RecordSaleRequest(
        Guid AnimalId, DateOnly SaleDate, string Buyer,
        decimal PriceTotal, decimal? PricePerKg, decimal? WeightKg,
        decimal Commission, decimal Transport, string? PaperworkReference, string? Notes);

    [HttpPost("sales")]
    public async Task<IActionResult> Sell([FromBody] RecordSaleRequest body, CancellationToken ct)
    {
        var id = await mediator.Send(new RecordSaleCommand(
            body.AnimalId, body.SaleDate, body.Buyer, body.PriceTotal, body.PricePerKg,
            body.WeightKg, body.Commission, body.Transport, body.PaperworkReference, body.Notes), ct);
        return Created($"/api/v1/commerce/sales/{id}", new { id });
    }

    public sealed record RecordDeathRequest(
        Guid AnimalId, DateOnly DeathDate, DeathCause Cause,
        string? SuspectedDisease, string? PostMortemFindings, bool InsuranceClaimable, string? Notes);

    [HttpPost("deaths")]
    public async Task<IActionResult> Death([FromBody] RecordDeathRequest body, CancellationToken ct)
    {
        var id = await mediator.Send(new RecordDeathCommand(
            body.AnimalId, body.DeathDate, body.Cause, body.SuspectedDisease,
            body.PostMortemFindings, body.InsuranceClaimable, body.Notes), ct);
        return Created($"/api/v1/commerce/deaths/{id}", new { id });
    }
}
