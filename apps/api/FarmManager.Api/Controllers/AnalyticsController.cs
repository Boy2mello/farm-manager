using FarmManager.Application.Analytics.Intelligence;
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
    // ---------- Phase D legacy ----------

    [HttpGet("kpis")]
    public async Task<IActionResult> Kpis(CancellationToken ct)
        => Ok(await mediator.Send(new HerdKpisQuery(), ct));

    [HttpGet("trends/{metric}")]
    public async Task<IActionResult> Trend(string metric, [FromQuery] int days = 90, CancellationToken ct = default)
        => Ok(await mediator.Send(new MetricTrendQuery(metric, days), ct));

    [HttpGet("underperformers")]
    public async Task<IActionResult> Underperformers([FromQuery] string? flag = null, CancellationToken ct = default)
        => Ok(await mediator.Send(new UnderperformersQuery(flag), ct));

    // ---------- Intelligence layer ----------

    [HttpGet("intelligence/herd")]
    public async Task<IActionResult> HerdIntel(CancellationToken ct)
        => Ok(await mediator.Send(new HerdIntelligenceQuery(), ct));

    [HttpGet("intelligence/bulls")]
    public async Task<IActionResult> BullIntel(CancellationToken ct)
        => Ok(await mediator.Send(new BullIntelligenceQuery(), ct));

    [HttpGet("intelligence/bloodlines")]
    public async Task<IActionResult> BloodlineIntel(CancellationToken ct)
        => Ok(await mediator.Send(new BloodlineIntelligenceQuery(), ct));

    [HttpGet("intelligence/years")]
    public async Task<IActionResult> Years(CancellationToken ct)
        => Ok(await mediator.Send(new YearPerformanceQuery(), ct));

    [HttpGet("intelligence/insights")]
    public async Task<IActionResult> Insights(CancellationToken ct)
        => Ok(await mediator.Send(new InsightsFeedQuery(), ct));
}
