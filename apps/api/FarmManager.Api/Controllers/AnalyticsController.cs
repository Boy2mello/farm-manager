using FarmManager.Application.Analytics.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManager.Api.Controllers;

[ApiController]
[Route("api/v1/analytics")]
[Authorize]
public sealed class AnalyticsController(ISender mediator) : ControllerBase
{
    [HttpGet("kpis")]
    public async Task<IActionResult> Kpis(CancellationToken ct)
        => Ok(await mediator.Send(new HerdKpisQuery(), ct));

    [HttpGet("trends/{metric}")]
    public async Task<IActionResult> Trend(string metric, [FromQuery] int days = 90, CancellationToken ct = default)
        => Ok(await mediator.Send(new MetricTrendQuery(metric, days), ct));

    [HttpGet("underperformers")]
    public async Task<IActionResult> Underperformers([FromQuery] string? flag = null, CancellationToken ct = default)
        => Ok(await mediator.Send(new UnderperformersQuery(flag), ct));
}
