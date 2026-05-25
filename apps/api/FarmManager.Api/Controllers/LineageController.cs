using FarmManager.Application.Lineage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManager.Api.Controllers;

[ApiController]
[Route("api/v1/lineage")]
[Authorize]
public sealed class LineageController(IInbreedingCalculator inbreeding) : ControllerBase
{
    /// <summary>
    /// Returns the inbreeding F for a hypothetical mating, plus the policy verdict per spec §12.3.
    /// </summary>
    [HttpGet("inbreeding")]
    public async Task<IActionResult> Inbreeding([FromQuery] Guid sireId, [FromQuery] Guid damId, CancellationToken ct)
    {
        var f = await inbreeding.ComputeAsync(sireId, damId, maxGenerations: 6, ct);
        var verdict = InbreedingPolicy.Evaluate(f);
        return Ok(new
        {
            f = verdict.F,
            action = verdict.Action.ToString(),
            reason = verdict.Reason,
        });
    }
}
