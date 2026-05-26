using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using FarmManager.Application.Animals;
using FarmManager.Application.Common.Interfaces;
using FarmManager.Application.Flagging;
using FarmManager.Application.Imports;
using FarmManager.Domain.Animals;
using FarmManager.Domain.Breeding;
using FarmManager.Domain.Commerce;
using FarmManager.Domain.Flagging;
using FarmManager.Domain.Organisations;
using FarmManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmManager.Infrastructure.Imports;

/// <summary>
/// Imports the user's <c>Livestock_Register.xlsx</c> workbook into the Farm Manager database.
///
/// The workbook has 13 sheets; this importer reads:
/// <list type="bullet">
///   <item><term>Master Register</term><description>40 active animals + breeds + DOBs + parents</description></item>
///   <item><term>Lineage</term><description>mother→calf rows used to emit historical calving events</description></item>
///   <item><term>Breeding Status</term><description>Open / confirmed-pregnant / covered cows</description></item>
///   <item><term>Calving Calendar</term><description>14 confirmed pregnancies with due dates</description></item>
///   <item><term>Performance Ranking</term><description>per-cow tier A–E + reasoning</description></item>
///   <item><term>Sold-Historic</term><description>5 sold bull calves + 3 deaths</description></item>
///   <item><term>Bull Calf Plan / Heifer Calf Readiness / First Time Mothers</term>
///   <description>informational — derivable, not re-imported</description></item>
/// </list>
/// Idempotent — animals are matched by (organisation, primary name) and events by
/// (animal, event type, event date). Re-runs add nothing new.
/// </summary>
public sealed class LivestockRegisterImporter(
    FarmManagerDbContext db,
    ICodeNameGenerator codeNames,
    ILogger<LivestockRegisterImporter> logger) : ILivestockRegisterImporter
{
    // Canonical name → list of accepted spellings (case-insensitive). Pulled from the workbook README
    // and the Animal aliases array in HerdData.cs.
    private static readonly Dictionary<string, string[]> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Mmadikrempe"] = new[] { "Tiki" },
        ["Smongo"] = new[] { "Smongonase", "Manki" },
        ["Mawick"] = new[] { "Obakeng" },
        ["Makantase Jr"] = new[] { "Junior" },
        ["Georgina"] = new[] { "Springbok" },
        ["Lerato Makantase"] = new[] { "Thando", "Lerato" },
        ["Lapi"] = new[] { "Lapa" },
        ["Poelo"] = new[] { "Matlhale" },
        ["Amogelang"] = new[] { "Amo" },
        ["Nandipha Magudumana"] = new[] { "Nandipha" },
        ["Bali"] = new[] { "mebala" },
    };

    private const string ResidentBullName = "Boshomane";

    public async Task<ImportReport> ImportAsync(string excelPath, string organisationName, CancellationToken ct = default)
    {
        if (!File.Exists(excelPath))
        {
            throw new FileNotFoundException("Livestock register workbook not found.", excelPath);
        }

        logger.LogInformation("Livestock register import starting from {Path}", excelPath);

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var org = await EnsureOrganisationAsync(organisationName, ct);
        var report = new ImportReport
        {
            OrganisationId = org.Id,
            OrganisationName = org.Name,
            SourceFilePath = excelPath,
        };

        using var workbook = new XLWorkbook(excelPath);

        var animalCache = await LoadExistingAnimalsAsync(org.Id, ct);
        await EnsureFarmsAsync(org.Id, workbook, animalCache, report, ct);

        // ----- Resident bull (Boshomane) -----
        var residentBullId = await EnsureResidentBullAsync(org.Id, animalCache, report, ct);

        // ----- Master Register -----
        await ImportMasterRegisterAsync(workbook, org, animalCache, report, ct);

        // ----- Dam Mphonyana (referenced as Bali's mother but not in active register) -----
        await EnsureDeceasedDamMphonyanaAsync(org, animalCache, report, ct);

        // Reload + link parents.
        animalCache = await LoadExistingAnimalsAsync(org.Id, ct);
        await LinkParentsAsync(workbook, animalCache, residentBullId, report, ct);

        // ----- Lineage → calving events -----
        await ImportLineageAsCalvingsAsync(workbook, org.Id, animalCache, residentBullId, report, ct);

        // ----- Breeding status (open / pregnant / covered) -----
        await ImportBreedingStatusAsync(workbook, org.Id, animalCache, residentBullId, report, ct);

        // ----- Calving Calendar (due dates) -----
        await ImportCalvingCalendarAsync(workbook, org.Id, animalCache, report, ct);

        // ----- Performance Ranking → tier + tier assignment row -----
        await ImportPerformanceRankingAsync(workbook, org.Id, animalCache, report, ct);

        // ----- Sold-Historic → sale + death events -----
        await ImportSoldHistoricAsync(workbook, org.Id, animalCache, report, ct);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        report.FinishedAt = DateTimeOffset.UtcNow;
        logger.LogInformation("Livestock register import finished: {Summary}", report.Summarise());
        return report;
    }

    // ---------------------------------------------------------------------
    // Section loaders
    // ---------------------------------------------------------------------

    private async Task<Organisation> EnsureOrganisationAsync(string name, CancellationToken ct)
    {
        var org = await db.Organisations.FirstOrDefaultAsync(o => o.Name == name, ct);
        if (org is not null) return org;

        org = Organisation.Create(name);
        db.Organisations.Add(org);
        await db.SaveChangesAsync(ct);
        return org;
    }

    private async Task EnsureFarmsAsync(Guid orgId, XLWorkbook workbook, Dictionary<string, Animal> animalCache, ImportReport report, CancellationToken ct)
    {
        // Owner Summary sheet lists Jijo / Olly / Tumi as the three sub-herds.
        var farmNames = new[] { "Tumi", "Jijo", "Olly" };
        foreach (var name in farmNames)
        {
            var exists = await db.Farms.AnyAsync(f => f.OrganisationId == orgId && f.Name == name, ct);
            if (exists) continue;

            var farm = Farm.Create(orgId, name, ownerName: name,
                notes: "Sub-herd seeded from Livestock_Register.xlsx Owner Summary.");
            db.Farms.Add(farm);
            report.FarmsCreated++;
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task<Guid> EnsureResidentBullAsync(Guid orgId, Dictionary<string, Animal> animalCache, ImportReport report, CancellationToken ct)
    {
        if (animalCache.TryGetValue(Normalise(ResidentBullName), out var bull))
        {
            return bull.Id;
        }

        var code = await codeNames.NextAsync(orgId, AnimalSource.Legacy, 2015, ct);
        var animal = Animal.Register(
            organisationId: orgId,
            codeName: code,
            sex: AnimalSex.Male,
            dob: new DateOnly(2015, 1, 1),
            dobPrecision: DobPrecision.Year,
            source: AnimalSource.Legacy,
            primaryName: ResidentBullName);
        db.Animals.Add(animal);
        await db.SaveChangesAsync(ct);
        animalCache[Normalise(ResidentBullName)] = animal;
        report.AnimalsCreated++;
        return animal.Id;
    }

    private async Task EnsureDeceasedDamMphonyanaAsync(Organisation org, Dictionary<string, Animal> animalCache, ImportReport report, CancellationToken ct)
    {
        const string name = "Mphonyana";
        if (animalCache.ContainsKey(Normalise(name))) return;

        var code = await codeNames.NextAsync(org.Id, AnimalSource.Legacy, 2018, ct);
        var dam = Animal.Register(
            organisationId: org.Id,
            codeName: code,
            sex: AnimalSex.Female,
            dob: new DateOnly(2018, 1, 1),
            dobPrecision: DobPrecision.Year,
            source: AnimalSource.Legacy,
            primaryName: name);
        dam.TransitionTo(AnimalStatus.Dead);
        db.Animals.Add(dam);

        db.DeathEvents.Add(DeathEvent.Create(
            organisationId: org.Id,
            animalId: dam.Id,
            deathDate: new DateOnly(2025, 2, 2),
            cause: DeathCause.Lightning,
            suspectedDisease: null,
            postMortemFindings: null,
            insuranceClaimable: false,
            notes: "Struck by lightning. Source: Sold-Historic + History Notes.",
            createdBy: "import:livestock-register"));
        report.DeathEventsCreated++;

        await db.SaveChangesAsync(ct);
        animalCache[Normalise(name)] = dam;
        report.AnimalsCreated++;
    }

    private async Task ImportMasterRegisterAsync(XLWorkbook workbook, Organisation org, Dictionary<string, Animal> animalCache, ImportReport report, CancellationToken ct)
    {
        var sheet = workbook.Worksheet("Master Register");
        var rows = sheet.RowsUsed().Skip(3); // headers are on row 3 (1-indexed) — data starts at row 4.

        foreach (var row in rows)
        {
            var first = row.Cell(1).GetString().Trim();
            if (string.IsNullOrWhiteSpace(first) || !int.TryParse(first, out _))
            {
                continue; // skip blank + footer rows
            }

            var name = row.Cell(2).GetString().Trim();
            if (string.IsNullOrWhiteSpace(name)) continue;

            if (animalCache.ContainsKey(Normalise(name)))
            {
                report.AnimalsSkippedExisting++;
                continue;
            }

            var aliasField = row.Cell(3).GetString().Trim();
            var breed = row.Cell(4).GetString().Trim();
            var sex = ParseSex(row.Cell(5).GetString());
            var dobText = row.Cell(6).GetString().Trim();
            var birthYearText = row.Cell(7).GetString().Trim();
            var bSiredFlag = string.Equals(row.Cell(10).GetString().Trim(), "Y", StringComparison.OrdinalIgnoreCase);

            var (dob, precision) = ParseDob(dobText, birthYearText);
            var aliases = ParseAliases(aliasField);

            var code = await codeNames.NextAsync(org.Id, AnimalSource.Legacy, dob.Year, ct);
            var animal = Animal.Register(
                organisationId: org.Id,
                codeName: code,
                sex: sex,
                dob: dob,
                dobPrecision: precision,
                source: AnimalSource.Legacy,
                primaryName: name,
                aliases: aliases);

            if (bSiredFlag) animal.MarkAsBSired();
            db.Animals.Add(animal);
            animalCache[Normalise(name)] = animal;
            foreach (var alias in aliases)
            {
                animalCache[Normalise(alias)] = animal;
            }
            report.AnimalsCreated++;
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task LinkParentsAsync(XLWorkbook workbook, Dictionary<string, Animal> animalCache, Guid residentBullId, ImportReport report, CancellationToken ct)
    {
        var sheet = workbook.Worksheet("Master Register");
        var rows = sheet.RowsUsed().Skip(3);

        foreach (var row in rows)
        {
            var first = row.Cell(1).GetString().Trim();
            if (string.IsNullOrWhiteSpace(first) || !int.TryParse(first, out _)) continue;

            var name = row.Cell(2).GetString().Trim();
            if (!animalCache.TryGetValue(Normalise(name), out var animal)) continue;

            var damName = StripAnnotations(row.Cell(8).GetString().Trim());
            var sireName = StripAnnotations(row.Cell(9).GetString().Trim());

            Guid? damId = ResolveAnimalId(damName, animalCache, report);
            Guid? sireId = string.Equals(sireName, ResidentBullName, StringComparison.OrdinalIgnoreCase)
                ? residentBullId
                : ResolveAnimalId(sireName, animalCache, report);

            if (damId == animal.DamId && sireId == animal.SireId) continue;

            // Apply both at once via raw SQL since the aggregate doesn't expose parent setters.
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE animals SET dam_id = {1}, sire_id = {2}, updated_at = NOW() WHERE id = {0}",
                animal.Id,
                damId is null ? (object)DBNull.Value : damId.Value,
                sireId is null ? (object)DBNull.Value : sireId.Value);
        }
    }

    private async Task ImportLineageAsCalvingsAsync(XLWorkbook workbook, Guid orgId, Dictionary<string, Animal> animalCache, Guid residentBullId, ImportReport report, CancellationToken ct)
    {
        var sheet = workbook.Worksheet("Lineage");
        var rows = sheet.RowsUsed().Skip(3);

        foreach (var row in rows)
        {
            var motherName = StripAnnotations(row.Cell(1).GetString().Trim());
            var calfName = row.Cell(2).GetString().Trim();
            var dobText = row.Cell(4).GetString().Trim();
            var sireName = StripAnnotations(row.Cell(5).GetString().Trim());

            if (string.IsNullOrWhiteSpace(motherName) || string.IsNullOrWhiteSpace(calfName)) continue;

            var dam = ResolveAnimal(motherName, animalCache, report);
            var calf = ResolveAnimal(calfName, animalCache, report);
            if (dam is null || calf is null) continue;

            if (!TryParseExactDate(dobText, out var calvingDate))
            {
                calvingDate = calf.Dob;
            }

            var exists = await db.CalvingEvents
                .AnyAsync(c => c.DamId == dam.Id && c.CalfId == calf.Id, ct);
            if (exists) continue;

            Guid? sireId = string.Equals(sireName, ResidentBullName, StringComparison.OrdinalIgnoreCase)
                ? residentBullId
                : ResolveAnimalId(sireName, animalCache, report);

            db.CalvingEvents.Add(CalvingEvent.Create(
                organisationId: orgId,
                damId: dam.Id,
                calfId: calf.Id,
                calvingDate: calvingDate,
                sireId: sireId,
                sireExternalNote: null,
                difficultyScore: 1,
                assistanceRequired: false,
                placentaDelivered: true,
                motheringAbility: null,
                stillbirth: false,
                calfWeightKg: null,
                calfVigour: null,
                notes: "Imported from Lineage sheet of Livestock_Register.xlsx.",
                createdBy: "import:livestock-register"));
            report.CalvingEventsCreated++;

            // Mark calf as B-sired if the sire was Boshomane.
            if (sireId == residentBullId && !calf.IsBSired)
            {
                calf.MarkAsBSired();
            }
        }
    }

    private async Task ImportBreedingStatusAsync(XLWorkbook workbook, Guid orgId, Dictionary<string, Animal> animalCache, Guid residentBullId, ImportReport report, CancellationToken ct)
    {
        var sheet = workbook.Worksheet("Breeding Status");
        var openSection = false;
        var pregnantSection = false;
        var coveredSection = false;

        foreach (var row in sheet.RowsUsed())
        {
            var a = row.Cell(1).GetString().Trim();
            var b = row.Cell(2).GetString().Trim();

            if (a.StartsWith("Open Cows", StringComparison.OrdinalIgnoreCase)) { openSection = true; pregnantSection = false; coveredSection = false; continue; }
            if (a.StartsWith("Confirmed Pregnant", StringComparison.OrdinalIgnoreCase)) { openSection = false; pregnantSection = true; coveredSection = false; continue; }
            if (a.StartsWith("Covered Cows", StringComparison.OrdinalIgnoreCase)) { openSection = false; pregnantSection = false; coveredSection = true; continue; }

            if (string.IsNullOrWhiteSpace(a) || !int.TryParse(a, out _)) continue;

            var cowName = b;
            var cow = ResolveAnimal(cowName, animalCache, report);
            if (cow is null) continue;

            if (openSection)
            {
                cow.TransitionTo(AnimalStatus.Open);
            }
            else if (pregnantSection)
            {
                cow.TransitionTo(AnimalStatus.PregnantConfirmed);
                var serviceDateText = row.Cell(4).GetString().Trim();
                if (TryParseExactDate(serviceDateText, out var serviceDate))
                {
                    await EnsureServiceAndPregnancyCheckAsync(orgId, cow, serviceDate, residentBullId, report, ct);
                }
            }
            else if (coveredSection)
            {
                var coveredDateText = row.Cell(3).GetString().Trim();
                var status = row.Cell(4).GetString().Trim();
                if (TryParseExactDate(coveredDateText, out var serviceDate))
                {
                    await EnsureServiceAndPregnancyCheckAsync(
                        orgId, cow, serviceDate, residentBullId, report, ct,
                        confirmed: status.Equals("Confirmed", StringComparison.OrdinalIgnoreCase));
                }
            }
        }
    }

    private async Task EnsureServiceAndPregnancyCheckAsync(
        Guid orgId,
        Animal cow,
        DateOnly serviceDate,
        Guid residentBullId,
        ImportReport report,
        CancellationToken ct,
        bool confirmed = true)
    {
        var serviceExists = await db.ServiceEvents
            .AnyAsync(s => s.CowId == cow.Id && s.ServiceDate == serviceDate, ct);
        if (!serviceExists)
        {
            db.ServiceEvents.Add(ServiceEvent.Create(
                organisationId: orgId,
                cowId: cow.Id,
                serviceDate: serviceDate,
                type: ServiceType.NaturalBull,
                bullId: residentBullId,
                aiStrawId: null,
                inbreedingCoefficient: null,
                notes: "Imported from Breeding Status sheet.",
                createdBy: "import:livestock-register"));
            report.ServiceEventsCreated++;
        }

        if (!confirmed) return;

        var checkDate = serviceDate.AddDays(60);
        var checkExists = await db.PregnancyCheckEvents
            .AnyAsync(p => p.CowId == cow.Id && p.CheckDate == checkDate, ct);
        if (!checkExists)
        {
            db.PregnancyCheckEvents.Add(PregnancyCheckEvent.Create(
                organisationId: orgId,
                cowId: cow.Id,
                checkDate: checkDate,
                method: PregnancyCheckMethod.Palpation,
                result: PregnancyCheckResult.Positive,
                daysBred: 60,
                vetUserId: null,
                notes: "Imported confirmation (back-dated from Breeding Status sheet).",
                createdBy: "import:livestock-register"));
            report.PregnancyCheckEventsCreated++;
        }
    }

    private async Task ImportCalvingCalendarAsync(XLWorkbook workbook, Guid orgId, Dictionary<string, Animal> animalCache, ImportReport report, CancellationToken ct)
    {
        var sheet = workbook.Worksheet("Calving Calendar");
        foreach (var row in sheet.RowsUsed())
        {
            var a = row.Cell(1).GetString().Trim();
            if (string.IsNullOrWhiteSpace(a) || !int.TryParse(a, out _)) continue;

            var cowName = row.Cell(2).GetString().Trim();
            var dueText = row.Cell(3).GetString().Trim();
            var cow = ResolveAnimal(cowName, animalCache, report);
            if (cow is null) continue;

            if (!TryParseExactDate(dueText, out var due)) continue;

            // Persist the expected calving date by upserting a service event back-dated 283 days
            // if no service exists yet.
            var serviceDate = due.AddDays(-ServiceEvent.GestationDays);
            var existingService = await db.ServiceEvents
                .AnyAsync(s => s.CowId == cow.Id && s.ServiceDate == serviceDate, ct);
            if (!existingService)
            {
                db.ServiceEvents.Add(ServiceEvent.Create(
                    organisationId: orgId,
                    cowId: cow.Id,
                    serviceDate: serviceDate,
                    type: ServiceType.NaturalBull,
                    bullId: null,
                    aiStrawId: null,
                    inbreedingCoefficient: null,
                    notes: $"Synthetic — derived from Calving Calendar due {due:yyyy-MM-dd}.",
                    createdBy: "import:livestock-register"));
                report.ServiceEventsCreated++;
            }

            if (cow.Status != AnimalStatus.PregnantConfirmed)
            {
                cow.TransitionTo(AnimalStatus.PregnantConfirmed);
            }
        }
    }

    private async Task ImportPerformanceRankingAsync(XLWorkbook workbook, Guid orgId, Dictionary<string, Animal> animalCache, ImportReport report, CancellationToken ct)
    {
        var sheet = workbook.Worksheet("Performance Ranking");
        foreach (var row in sheet.RowsUsed())
        {
            var rankText = row.Cell(1).GetString().Trim();
            if (string.IsNullOrWhiteSpace(rankText) || !int.TryParse(rankText, out _)) continue;

            var cowName = row.Cell(2).GetString().Trim();
            var tierText = row.Cell(16).GetString().Trim();
            var notes = row.Cell(18).GetString().Trim();

            var cow = ResolveAnimal(cowName, animalCache, report);
            if (cow is null) continue;

            var tier = tierText.ToUpperInvariant() switch
            {
                "A" => PerformanceTier.A,
                "B" => PerformanceTier.B,
                "C" => PerformanceTier.C,
                "D" => PerformanceTier.D,
                "E" => PerformanceTier.E,
                _ => PerformanceTier.None,
            };
            if (tier == PerformanceTier.None) continue;

            if (cow.PerformanceTier == tier)
            {
                // Already at this tier — no event needed.
                continue;
            }

            cow.UpdatePerformance(cow.CalfCount, cow.CalvesAlive, cow.AvgCalvingIntervalDays, cow.CalvesPerYear, tier);

            db.TierAssignments.Add(TierAssignment.Record(
                organisationId: orgId,
                animalId: cow.Id,
                previous: PerformanceTier.None,
                current: tier,
                reason: $"Seeded from Performance Ranking sheet · Tier {tierText}. {notes}".Trim(),
                metricsJson: "{}",
                assignedBy: "import:livestock-register"));
            report.TierAssignmentsCreated++;
        }
    }

    private async Task ImportSoldHistoricAsync(XLWorkbook workbook, Guid orgId, Dictionary<string, Animal> animalCache, ImportReport report, CancellationToken ct)
    {
        var sheet = workbook.Worksheet("Sold-Historic");
        foreach (var row in sheet.RowsUsed())
        {
            var name = row.Cell(1).GetString().Trim();
            if (string.IsNullOrWhiteSpace(name) || name.StartsWith("Name", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("Sold,", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("Pending", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("Obakeng", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var cleanName = Regex.Replace(name, @"\s*\(.*?\)\s*$", "").Trim();
            var sex = ParseSex(row.Cell(2).GetString());
            var dateText = row.Cell(3).GetString().Trim();
            var disposition = row.Cell(4).GetString().Trim();
            var notes = row.Cell(5).GetString().Trim();

            if (string.Equals(cleanName, "30 goats", StringComparison.OrdinalIgnoreCase))
            {
                report.Warnings.Add("Ignoring '30 goats stolen Nov 2023' — non-cattle event.");
                continue;
            }

            var animal = ResolveAnimal(cleanName, animalCache, report);
            if (animal is null)
            {
                // The Sold-Historic rows reference animals that are NOT in the active register
                // (Russell, Gijima etc). Synthesize them.
                animal = await SynthesizeSoldAnimalAsync(orgId, cleanName, sex, dateText, ct);
                animalCache[Normalise(cleanName)] = animal;
                report.AnimalsCreated++;
            }

            if (TryParseRelaxedDate(dateText, out var eventDate))
            {
                if (disposition.Contains("Sold", StringComparison.OrdinalIgnoreCase) ||
                    disposition.Contains("Slaughtered", StringComparison.OrdinalIgnoreCase))
                {
                    if (!await db.SaleEvents.AnyAsync(s => s.AnimalId == animal.Id, ct))
                    {
                        db.SaleEvents.Add(SaleEvent.Create(
                            organisationId: orgId,
                            animalId: animal.Id,
                            saleDate: eventDate,
                            buyer: disposition.Contains("Slaughtered", StringComparison.OrdinalIgnoreCase) ? "Self (slaughtered)" : "Historical — buyer unknown",
                            priceTotal: 0.01m,
                            pricePerKg: null,
                            weightKg: null,
                            commission: 0m,
                            transport: 0m,
                            paperworkReference: null,
                            notes: notes,
                            createdBy: "import:livestock-register"));
                        report.SaleEventsCreated++;
                        animal.TransitionTo(AnimalStatus.Sold);
                    }
                }
                else if (disposition.Contains("Deceased", StringComparison.OrdinalIgnoreCase) ||
                         disposition.Contains("Died", StringComparison.OrdinalIgnoreCase))
                {
                    if (!await db.DeathEvents.AnyAsync(d => d.AnimalId == animal.Id, ct))
                    {
                        var cause = notes.Contains("lightning", StringComparison.OrdinalIgnoreCase) ? DeathCause.Lightning
                            : notes.Contains("birth", StringComparison.OrdinalIgnoreCase) ? DeathCause.Calving
                            : DeathCause.Unknown;

                        db.DeathEvents.Add(DeathEvent.Create(
                            organisationId: orgId,
                            animalId: animal.Id,
                            deathDate: eventDate,
                            cause: cause,
                            suspectedDisease: null,
                            postMortemFindings: null,
                            insuranceClaimable: false,
                            notes: notes,
                            createdBy: "import:livestock-register"));
                        report.DeathEventsCreated++;
                        animal.TransitionTo(AnimalStatus.Dead);
                    }
                }
                else if (disposition.Contains("Stolen", StringComparison.OrdinalIgnoreCase))
                {
                    animal.TransitionTo(AnimalStatus.Missing);
                }
            }
            else
            {
                report.Warnings.Add($"Sold-Historic row '{cleanName}' has no parseable date ('{dateText}') — status set without event.");
                if (disposition.Contains("Sold", StringComparison.OrdinalIgnoreCase)) animal.TransitionTo(AnimalStatus.Sold);
                if (disposition.Contains("Stolen", StringComparison.OrdinalIgnoreCase)) animal.TransitionTo(AnimalStatus.Missing);
            }
        }
    }

    private async Task<Animal> SynthesizeSoldAnimalAsync(Guid orgId, string name, AnimalSex sex, string dateText, CancellationToken ct)
    {
        var dob = TryParseRelaxedDate(dateText, out var d) ? new DateOnly(d.Year, 1, 1) : new DateOnly(2024, 1, 1);
        var code = await codeNames.NextAsync(orgId, AnimalSource.Legacy, dob.Year, ct);
        var animal = Animal.Register(
            organisationId: orgId,
            codeName: code,
            sex: sex,
            dob: dob,
            dobPrecision: DobPrecision.Year,
            source: AnimalSource.Legacy,
            primaryName: name);
        db.Animals.Add(animal);
        await db.SaveChangesAsync(ct);
        return animal;
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    private async Task<Dictionary<string, Animal>> LoadExistingAnimalsAsync(Guid orgId, CancellationToken ct)
    {
        var existing = await db.Animals
            .Where(a => a.OrganisationId == orgId)
            .ToListAsync(ct);

        var cache = new Dictionary<string, Animal>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in existing)
        {
            if (!string.IsNullOrWhiteSpace(a.PrimaryName))
            {
                cache[Normalise(a.PrimaryName)] = a;
            }
            foreach (var alias in a.Aliases)
            {
                cache[Normalise(alias)] = a;
            }
        }
        return cache;
    }

    private static string Normalise(string raw) =>
        raw.Trim().ToLowerInvariant();

    private static AnimalSex ParseSex(string raw) =>
        string.Equals(raw.Trim(), "M", StringComparison.OrdinalIgnoreCase)
            ? AnimalSex.Male
            : AnimalSex.Female;

    private static (DateOnly Dob, DobPrecision Precision) ParseDob(string dobText, string birthYearText)
    {
        if (TryParseExactDate(dobText, out var exact))
        {
            return (exact, DobPrecision.Day);
        }

        // "Apr 2023" / "Jun 2020" / "Oct 2020" → month precision
        if (DateTime.TryParseExact(dobText, new[] { "MMM yyyy", "MMMM yyyy" },
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var month))
        {
            return (new DateOnly(month.Year, month.Month, 1), DobPrecision.Month);
        }

        // Year-only e.g. "2020"
        if (int.TryParse(string.IsNullOrWhiteSpace(dobText) ? birthYearText : dobText, out var year)
            && year is > 1900 and < 2100)
        {
            return (new DateOnly(year, 1, 1), DobPrecision.Year);
        }

        return (new DateOnly(2020, 1, 1), DobPrecision.Year);
    }

    private static bool TryParseExactDate(string raw, out DateOnly date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        if (raw.Equals("(unknown)", StringComparison.OrdinalIgnoreCase)) return false;

        var formats = new[]
        {
            "yyyy-MM-dd",
            "d MMM yyyy",
            "dd MMM yyyy",
            "d MMMM yyyy",
            "dd/MM/yyyy",
            "d/M/yyyy",
            "M/d/yyyy",
            "MM/dd/yyyy",
        };
        if (DateTime.TryParseExact(raw, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            date = DateOnly.FromDateTime(dt);
            return true;
        }
        return false;
    }

    private static bool TryParseRelaxedDate(string raw, out DateOnly date)
    {
        if (TryParseExactDate(raw, out date)) return true;
        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            date = DateOnly.FromDateTime(dt);
            return true;
        }
        date = default;
        return false;
    }

    private static IReadOnlyList<string> ParseAliases(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return Array.Empty<string>();
        return raw.Split(new[] { '/', ',', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string StripAnnotations(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return raw;
        return Regex.Replace(raw, @"\s*\(.*?\)\s*$", "").Trim();
    }

    private static Animal? ResolveAnimal(string raw, Dictionary<string, Animal> cache, ImportReport report)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var key = Normalise(raw);
        if (cache.TryGetValue(key, out var hit)) return hit;

        // Search the alias table.
        foreach (var (canonical, aliases) in Aliases)
        {
            var canonicalKey = Normalise(canonical);
            if (canonicalKey == key && cache.TryGetValue(canonicalKey, out var c)) return c;

            foreach (var alias in aliases)
            {
                if (Normalise(alias) == key && cache.TryGetValue(canonicalKey, out var byAlias))
                {
                    report.ResolvedAliases.Add($"'{raw}' → '{canonical}'");
                    return byAlias;
                }
            }
        }

        report.UnmatchedNames.Add(raw);
        return null;
    }

    private static Guid? ResolveAnimalId(string raw, Dictionary<string, Animal> cache, ImportReport report)
        => ResolveAnimal(raw, cache, report)?.Id;
}
