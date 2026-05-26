using ClosedXML.Excel;
using FluentAssertions;
using Xunit;

namespace FarmManager.Tests.Imports;

/// <summary>
/// Lightweight smoke tests that prove the shipping workbook still has the sheets the importer
/// depends on. If the workbook is missing from the test output dir these no-op rather than
/// fail — the importer itself errors loudly at runtime.
/// </summary>
public class WorkbookSmokeTests
{
    private static string? ResolveWorkbookPath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "docs", "Livestock_Register.xlsx"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "docs", "Livestock_Register.xlsx"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "Livestock_Register.xlsx"),
        };
        foreach (var c in candidates)
        {
            var full = Path.GetFullPath(c);
            if (File.Exists(full)) return full;
        }
        return null;
    }

    [Fact]
    public void Workbook_ships_with_the_expected_sheets()
    {
        var path = ResolveWorkbookPath();
        if (path is null) return; // workbook not in this test run's output — skip.

        using var wb = new XLWorkbook(path);
        var sheetNames = wb.Worksheets.Select(s => s.Name).ToHashSet();

        sheetNames.Should().Contain("Master Register");
        sheetNames.Should().Contain("Lineage");
        sheetNames.Should().Contain("Breeding Status");
        sheetNames.Should().Contain("Calving Calendar");
        sheetNames.Should().Contain("Performance Ranking");
        sheetNames.Should().Contain("Sold-Historic");
    }

    [Fact]
    public void Master_register_has_at_least_40_animals()
    {
        var path = ResolveWorkbookPath();
        if (path is null) return;

        using var wb = new XLWorkbook(path);
        var sheet = wb.Worksheet("Master Register");
        var rowsWithIds = sheet.RowsUsed()
            .Skip(3)
            .Where(r => int.TryParse(r.Cell(1).GetString(), out _))
            .ToList();

        rowsWithIds.Should().HaveCountGreaterOrEqualTo(40);
    }
}
