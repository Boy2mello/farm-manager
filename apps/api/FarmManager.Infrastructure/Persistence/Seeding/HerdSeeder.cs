using FarmManager.Domain.Animals;
using FarmManager.Domain.Organisations;
using Microsoft.EntityFrameworkCore;

namespace FarmManager.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds the real 40-head herd from <c>docs/Livestock Register.md</c>, plus the Boshomane
/// lineage constraints in spec §12.4 / Appendix C. Idempotent — safe to run on every startup.
/// </summary>
public static class HerdSeeder
{
    private const string OrganisationName = "Tumi's Farm";

    public static async Task SeedAsync(FarmManagerDbContext db, CancellationToken ct = default)
    {
        var org = await db.Organisations.FirstOrDefaultAsync(o => o.Name == OrganisationName, ct);
        if (org is null)
        {
            org = Organisation.Create(OrganisationName);
            db.Organisations.Add(org);
            await db.SaveChangesAsync(ct);
        }

        if (await db.Animals.AnyAsync(a => a.OrganisationId == org.Id, ct))
        {
            return; // Already seeded.
        }

        var legacy = HerdData.LegacyAnimals(org.Id);

        // First pass: insert without parent links so we can resolve dam/sire ids by name.
        foreach (var (animal, _, _) in legacy)
        {
            db.Animals.Add(animal);
        }
        await db.SaveChangesAsync(ct);

        // Second pass: resolve dam + sire by primary name and patch via raw SQL to avoid retracking.
        var byName = await db.Animals
            .Where(a => a.OrganisationId == org.Id)
            .ToDictionaryAsync(a => a.PrimaryName!, a => a.Id, ct);

        foreach (var (animal, damName, sireName) in legacy)
        {
            Guid? damId = damName is not null && byName.TryGetValue(damName, out var d) ? d : null;
            Guid? sireId = sireName is not null && byName.TryGetValue(sireName, out var s) ? s : null;

            if (damId is null && sireId is null) continue;

            await db.Database.ExecuteSqlRawAsync(
                "UPDATE animals SET dam_id = {1}, sire_id = {2}, is_b_sired = ({3}) WHERE id = {0}",
                animal.Id,
                damId is null ? (object)DBNull.Value : damId.Value,
                sireId is null ? (object)DBNull.Value : sireId.Value,
                sireName == "Boshomane");
        }
    }
}
