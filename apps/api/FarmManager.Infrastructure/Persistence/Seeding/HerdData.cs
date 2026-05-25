using FarmManager.Domain.Animals;

namespace FarmManager.Infrastructure.Persistence.Seeding;

/// <summary>
/// The 40-head live herd from <c>docs/Livestock Register.md</c>, plus the resident bull Boshomane
/// (spec Appendix C). Each row carries the dam + sire by primary name so the seeder can resolve
/// links in a second pass.
/// </summary>
internal static class HerdData
{
    public static IEnumerable<(Animal Animal, string? DamName, string? SireName)> LegacyAnimals(Guid organisationId)
    {
        int seq = 0;
        Animal Mk(
            string name,
            DateOnly dob,
            DobPrecision prec,
            AnimalSex sex,
            AnimalSource source = AnimalSource.Legacy,
            IEnumerable<string>? aliases = null)
        {
            seq++;
            var code = new CodeName("L", dob.Year > 2026 ? 2026 : 2026, seq, 3);
            return Animal.Register(
                organisationId: organisationId,
                codeName: code,
                sex: sex,
                dob: dob,
                dobPrecision: prec,
                source: source,
                primaryName: name,
                aliases: aliases);
        }

        // Resident bull (spec Appendix C — non-numbered, foundational).
        yield return (Mk("Boshomane", new(2015, 1, 1), DobPrecision.Year, AnimalSex.Male), null, null);

        // ----- Boran Cross -----
        yield return (Mk("Springbok", new(2020, 1, 1), DobPrecision.Month, AnimalSex.Female, aliases: new[] { "Georgina" }), null, null);
        yield return (Mk("Mpho", new(2021, 9, 1), DobPrecision.Month, AnimalSex.Female), null, null);

        // ----- Brahman × Boran -----
        yield return (Mk("Zondi", new(2020, 1, 1), DobPrecision.Year, AnimalSex.Female), null, null);
        yield return (Mk("Kgetlheng", new(2020, 1, 1), DobPrecision.Year, AnimalSex.Female), null, null);
        yield return (Mk("Mantabole", new(2020, 1, 1), DobPrecision.Year, AnimalSex.Female), null, null);
        yield return (Mk("Leruo", new(2023, 4, 1), DobPrecision.Month, AnimalSex.Female), "Kgetlheng", null);
        yield return (Mk("Amogelang", new(2023, 3, 1), DobPrecision.Month, AnimalSex.Female), "Mantabole", null);
        yield return (Mk("Bali", new(2024, 8, 26), DobPrecision.Day, AnimalSex.Female), "Mphonyana", "Boshomane");
        yield return (Mk("Madikizela", new(2024, 8, 27), DobPrecision.Day, AnimalSex.Female), "Springbok", "Boshomane");
        yield return (Mk("Tlhabi", new(2025, 8, 12), DobPrecision.Day, AnimalSex.Female), "Zondi", "Boshomane");
        yield return (Mk("Lele", new(2025, 8, 30), DobPrecision.Day, AnimalSex.Male), "Mantabole", "Boshomane");

        // ----- Boran × Simbra -----
        yield return (Mk("Mmapule", new(2023, 12, 11), DobPrecision.Day, AnimalSex.Female), "Zondi", null);

        // ----- Brahman -----
        yield return (Mk("Makantase", new(2017, 1, 1), DobPrecision.Year, AnimalSex.Female), null, null);
        yield return (Mk("Makantase Jr", new(2021, 11, 1), DobPrecision.Month, AnimalSex.Female, aliases: new[] { "Junior" }), "Makantase", null);
        yield return (Mk("Baizani", new(2020, 2, 1), DobPrecision.Month, AnimalSex.Female), null, null);
        yield return (Mk("Lerato Makantase", new(2023, 2, 3), DobPrecision.Day, AnimalSex.Female, aliases: new[] { "Lerato" }), "Makantase", null);
        yield return (Mk("Serena", new(2023, 1, 1), DobPrecision.Year, AnimalSex.Female), null, null);
        yield return (Mk("Venus", new(2023, 1, 1), DobPrecision.Year, AnimalSex.Female), null, null);
        yield return (Mk("Coco Gauff", new(2023, 1, 1), DobPrecision.Year, AnimalSex.Female), null, null);
        yield return (Mk("Nandipha Magudumana", new(2023, 1, 1), DobPrecision.Year, AnimalSex.Female, aliases: new[] { "Nandipha" }), null, null);
        yield return (Mk("Smongo", new(2024, 6, 22), DobPrecision.Day, AnimalSex.Female), "Makantase", "Boshomane");
        yield return (Mk("Mawick", new(2024, 10, 28), DobPrecision.Day, AnimalSex.Male, aliases: new[] { "Obakeng" }), "Baizani", null);
        yield return (Mk("Nomsa", new(2025, 10, 25), DobPrecision.Day, AnimalSex.Female), "Serena", "Boshomane");
        yield return (Mk("Amerko", new(2025, 11, 19), DobPrecision.Day, AnimalSex.Male), "Nandipha Magudumana", null);
        yield return (Mk("Puleng", new(2026, 1, 4), DobPrecision.Day, AnimalSex.Female), "Makantase", "Boshomane");

        // ----- Brahman Cross -----
        yield return (Mk("Mmadikrempe", new(2020, 1, 1), DobPrecision.Year, AnimalSex.Female, aliases: new[] { "Tiki" }), null, null);
        yield return (Mk("Surprise", new(2020, 10, 1), DobPrecision.Month, AnimalSex.Female), null, null);
        yield return (Mk("Witkouse", new(2021, 5, 1), DobPrecision.Month, AnimalSex.Female), null, null);
        yield return (Mk("Kalajane", new(2021, 1, 1), DobPrecision.Month, AnimalSex.Female), null, null);
        yield return (Mk("Maitlhwana", new(2022, 4, 1), DobPrecision.Month, AnimalSex.Female), null, null);
        yield return (Mk("Noxy", new(2023, 8, 18), DobPrecision.Day, AnimalSex.Female), "Lapi", "Boshomane");
        yield return (Mk("Poelo", new(2023, 12, 13), DobPrecision.Day, AnimalSex.Female, aliases: new[] { "Matlhale" }), "Kalajane", "Boshomane");
        yield return (Mk("Motlalepula", new(2024, 11, 14), DobPrecision.Day, AnimalSex.Female), "Lapi", "Boshomane");
        yield return (Mk("Rapula", new(2025, 2, 18), DobPrecision.Day, AnimalSex.Male), "Surprise", "Boshomane");
        yield return (Mk("Makaku", new(2025, 12, 16), DobPrecision.Day, AnimalSex.Male), "Lebese", "Boshomane");
        yield return (Mk("Mpolokeng", new(2025, 11, 6), DobPrecision.Day, AnimalSex.Female), "Maitlhwana", "Boshomane");
        yield return (Mk("Bontle", new(2026, 2, 20), DobPrecision.Day, AnimalSex.Female), "Kgetlheng", "Boshomane");
        yield return (Mk("Lesedi", new(2026, 5, 14), DobPrecision.Day, AnimalSex.Male), "Kalajane", "Boshomane");

        // ----- Mix Breed -----
        yield return (Mk("Lapi", new(2020, 6, 1), DobPrecision.Month, AnimalSex.Female, aliases: new[] { "Lapa" }), null, null);
        yield return (Mk("Lebese", new(2021, 12, 1), DobPrecision.Month, AnimalSex.Female), null, null);
    }
}
