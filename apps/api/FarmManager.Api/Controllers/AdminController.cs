using FarmManager.Application.Imports;
using FarmManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManager.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = Roles.SuperAdmin + "," + Roles.Owner)]
public sealed class AdminController(
    ILivestockRegisterImporter importer,
    ILogger<AdminController> logger) : ControllerBase
{
    /// <summary>
    /// Re-runs the livestock register importer using an uploaded workbook. The importer is
    /// idempotent — animals are matched by primary name + aliases; events by (animal, type, date).
    /// </summary>
    [HttpPost("import/livestock-register")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> ImportRegister(
        [FromForm] IFormFile workbook,
        [FromForm] string? organisationName,
        CancellationToken ct)
    {
        if (workbook is null || workbook.Length == 0)
        {
            return BadRequest(new { error = "No workbook uploaded." });
        }

        var tempPath = Path.Combine(Path.GetTempPath(), $"livestock-register-{Guid.NewGuid():N}.xlsx");
        try
        {
            await using (var fs = System.IO.File.Create(tempPath))
            {
                await workbook.CopyToAsync(fs, ct);
            }

            var report = await importer.ImportAsync(
                tempPath,
                string.IsNullOrWhiteSpace(organisationName) ? "Tumi's Farm" : organisationName,
                ct);

            logger.LogInformation("Admin import complete. {Summary}", report.Summarise());

            return Ok(new
            {
                succeeded = report.Succeeded,
                summary = report.Summarise(),
                report,
            });
        }
        finally
        {
            try { System.IO.File.Delete(tempPath); } catch { /* best-effort cleanup */ }
        }
    }
}
