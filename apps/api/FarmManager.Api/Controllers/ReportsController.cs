using FarmManager.Application.Common.Interfaces;
using FarmManager.Application.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManager.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize]
public sealed class ReportsController(IReportEngine engine, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("herd-census.pdf")]
    public async Task<IActionResult> HerdCensusPdf(CancellationToken ct)
    {
        var orgId = currentUser.OrganisationId
            ?? throw new InvalidOperationException("No organisation context.");
        var file = await engine.HerdCensusPdfAsync(orgId, ct);
        return File(file.Bytes, file.ContentType, file.FileName);
    }

    [HttpGet("herd-census.xlsx")]
    public async Task<IActionResult> HerdCensusExcel(CancellationToken ct)
    {
        var orgId = currentUser.OrganisationId
            ?? throw new InvalidOperationException("No organisation context.");
        var file = await engine.HerdCensusExcelAsync(orgId, ct);
        return File(file.Bytes, file.ContentType, file.FileName);
    }

    [HttpGet("performance-ranking.pdf")]
    public async Task<IActionResult> PerformanceRanking(CancellationToken ct)
    {
        var orgId = currentUser.OrganisationId
            ?? throw new InvalidOperationException("No organisation context.");
        var file = await engine.PerformanceRankingPdfAsync(orgId, ct);
        return File(file.Bytes, file.ContentType, file.FileName);
    }

    [HttpGet("cull-candidates.pdf")]
    public async Task<IActionResult> CullCandidates(CancellationToken ct)
    {
        var orgId = currentUser.OrganisationId
            ?? throw new InvalidOperationException("No organisation context.");
        var file = await engine.CullCandidatesPdfAsync(orgId, ct);
        return File(file.Bytes, file.ContentType, file.FileName);
    }
}
