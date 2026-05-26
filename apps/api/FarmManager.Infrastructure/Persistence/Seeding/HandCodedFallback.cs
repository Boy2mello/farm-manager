using FarmManager.Domain.Animals;
using FarmManager.Domain.Organisations;
using Microsoft.EntityFrameworkCore;

namespace FarmManager.Infrastructure.Persistence.Seeding;

/// <summary>
/// Minimal hand-coded fallback used when the canonical <c>Livestock_Register.xlsx</c> workbook is
/// absent (e.g. inside unit tests). Creates the organisation, three sub-herd farms, and the
/// resident bull so basic flows can run.
/// </summary>
internal static class HandCodedFallback
{
    public static async Task SeedAsync(FarmManagerDbContext db, Guid organisationId, CancellationToken ct = default)
    {
        // Farms
        foreach (var name in new[] { "Tumi", "Jijo", "Olly" })
        {
            if (!await db.Farms.AnyAsync(f => f.OrganisationId == organisationId && f.Name == name, ct))
            {
                db.Farms.Add(Farm.Create(organisationId, name, ownerName: name,
                    notes: "Hand-coded fallback (no Livestock_Register.xlsx available)."));
            }
        }

        // Resident bull — needed for inbreeding tests
        if (!await db.Animals.AnyAsync(a => a.OrganisationId == organisationId && a.PrimaryName == "Boshomane", ct))
        {
            db.Animals.Add(Animal.Register(
                organisationId: organisationId,
                codeName: new CodeName("L", 2015, 1, 3),
                sex: AnimalSex.Male,
                dob: new DateOnly(2015, 1, 1),
                dobPrecision: DobPrecision.Year,
                source: AnimalSource.Legacy,
                primaryName: "Boshomane"));
        }

        await db.SaveChangesAsync(ct);
    }
}
