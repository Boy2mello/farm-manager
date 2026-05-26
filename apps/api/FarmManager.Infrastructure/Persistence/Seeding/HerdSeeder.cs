using FarmManager.Application.Imports;
using FarmManager.Domain.Organisations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmManager.Infrastructure.Persistence.Seeding;

/// <summary>
/// First-boot seeding policy:
///   1. If <c>docs/Livestock_Register.xlsx</c> exists alongside the deployment, run the
///      <see cref="ILivestockRegisterImporter"/> against it — this is the canonical source.
///   2. Otherwise fall back to the hand-coded 40-head fixture in <see cref="HerdData"/>.
///
/// Either path is idempotent: animals are matched by primary name; rerunning is a no-op.
/// </summary>
public static class HerdSeeder
{
    private const string OrganisationName = "Tumi's Farm";

    /// <summary>
    /// Search the application's working directory for a livestock register workbook.
    /// </summary>
    public static string? FindRegisterWorkbook()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "docs", "Livestock_Register.xlsx"),
            Path.Combine(Directory.GetCurrentDirectory(), "docs", "Livestock_Register.xlsx"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "docs", "Livestock_Register.xlsx"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "docs", "Livestock_Register.xlsx"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "docs", "Livestock_Register.xlsx"),
            Path.Combine("/srv/farm-manager/data", "Livestock_Register.xlsx"),
        };

        foreach (var c in candidates)
        {
            var resolved = Path.GetFullPath(c);
            if (File.Exists(resolved)) return resolved;
        }
        return null;
    }

    public static async Task SeedAsync(
        FarmManagerDbContext db,
        ILivestockRegisterImporter importer,
        ILogger logger,
        CancellationToken ct = default)
    {
        var org = await db.Organisations.FirstOrDefaultAsync(o => o.Name == OrganisationName, ct);
        if (org is null)
        {
            org = Organisation.Create(OrganisationName);
            db.Organisations.Add(org);
            await db.SaveChangesAsync(ct);
        }

        var alreadyPopulated = await db.Animals.AnyAsync(a => a.OrganisationId == org.Id, ct);

        var workbook = FindRegisterWorkbook();
        if (workbook is not null)
        {
            logger.LogInformation("Seeding via Livestock_Register.xlsx at {Path}", workbook);
            var report = await importer.ImportAsync(workbook, OrganisationName, ct);
            logger.LogInformation("Import summary:\n{Summary}", report.Summarise());
            return;
        }

        if (alreadyPopulated)
        {
            logger.LogInformation("Herd already populated and no register workbook found — skipping fallback seed.");
            return;
        }

        // Fallback: hand-coded fixture (used in unit tests where the workbook isn't shipped).
        logger.LogWarning("Livestock_Register.xlsx not found — using hand-coded 40-head fixture.");
        await HandCodedFallback.SeedAsync(db, org.Id, ct);
    }
}
