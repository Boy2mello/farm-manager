namespace FarmManager.Application.Imports;

/// <summary>
/// Structured summary returned by <see cref="ILivestockRegisterImporter"/> after each run.
/// </summary>
public sealed class ImportReport
{
    public Guid OrganisationId { get; init; }
    public string OrganisationName { get; init; } = default!;
    public string SourceFilePath { get; init; } = default!;
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset FinishedAt { get; set; }

    public int AnimalsCreated { get; set; }
    public int AnimalsSkippedExisting { get; set; }
    public int FarmsCreated { get; set; }
    public int CalvingEventsCreated { get; set; }
    public int ServiceEventsCreated { get; set; }
    public int PregnancyCheckEventsCreated { get; set; }
    public int SaleEventsCreated { get; set; }
    public int DeathEventsCreated { get; set; }
    public int TierAssignmentsCreated { get; set; }

    public List<string> Warnings { get; } = new();
    public List<string> Errors { get; } = new();
    public List<string> ResolvedAliases { get; } = new();
    public List<string> UnmatchedNames { get; } = new();

    public bool Succeeded => Errors.Count == 0;

    public string Summarise()
    {
        var lines = new List<string>
        {
            $"Livestock register import — {OrganisationName} ({OrganisationId})",
            $"Source: {SourceFilePath}",
            $"Duration: {(FinishedAt - StartedAt).TotalSeconds:0.0}s",
            "",
            "Created:",
            $"  Farms:               {FarmsCreated}",
            $"  Animals:             {AnimalsCreated} (skipped {AnimalsSkippedExisting} already-present)",
            $"  Calving events:      {CalvingEventsCreated}",
            $"  Service events:      {ServiceEventsCreated}",
            $"  Pregnancy checks:    {PregnancyCheckEventsCreated}",
            $"  Sales:               {SaleEventsCreated}",
            $"  Deaths:              {DeathEventsCreated}",
            $"  Tier assignments:    {TierAssignmentsCreated}",
        };

        if (Warnings.Count > 0)
        {
            lines.Add("");
            lines.Add($"Warnings ({Warnings.Count}):");
            lines.AddRange(Warnings.Select(w => "  - " + w));
        }

        if (UnmatchedNames.Count > 0)
        {
            lines.Add("");
            lines.Add($"Unmatched names ({UnmatchedNames.Count}):");
            lines.AddRange(UnmatchedNames.Distinct().Select(n => "  - " + n));
        }

        if (Errors.Count > 0)
        {
            lines.Add("");
            lines.Add($"ERRORS ({Errors.Count}):");
            lines.AddRange(Errors.Select(e => "  - " + e));
        }

        return string.Join(Environment.NewLine, lines);
    }
}

public interface ILivestockRegisterImporter
{
    /// <summary>
    /// Imports the workbook at <paramref name="excelPath"/> into the organisation whose name matches
    /// <paramref name="organisationName"/> (created if absent). Idempotent: animals are matched by
    /// primary name + aliases; events by (animal, type, date).
    /// </summary>
    Task<ImportReport> ImportAsync(string excelPath, string organisationName, CancellationToken ct = default);
}
