using ClosedXML.Excel;
using FarmManager.Application.Common.Interfaces;
using FarmManager.Application.Reporting;
using FarmManager.Domain.Animals;
using FarmManager.Domain.Flagging;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FarmManager.Infrastructure.Reporting;

/// <summary>
/// Spec §15.1 report catalogue. PDFs via QuestPDF (community licence is auto-applied at startup);
/// Excel via ClosedXML.
/// </summary>
public sealed class ReportEngine(IFarmManagerDbContext db) : IReportEngine
{
    static ReportEngine()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<ReportFile> HerdCensusPdfAsync(Guid organisationId, CancellationToken ct = default)
    {
        var animals = await db.Animals.AsNoTracking()
            .Where(a => a.OrganisationId == organisationId && a.Status != AnimalStatus.Sold && a.Status != AnimalStatus.Dead)
            .OrderBy(a => a.CodeName)
            .ToListAsync(ct);

        var doc = Document.Create(c =>
        {
            c.Page(p =>
            {
                p.Margin(28);
                p.Size(PageSizes.A4);
                p.Header().Text($"Herd census · {DateTime.UtcNow:yyyy-MM-dd}").SemiBold().FontSize(14);
                p.Content().Table(t =>
                {
                    t.ColumnsDefinition(cd =>
                    {
                        cd.RelativeColumn(2);
                        cd.RelativeColumn(3);
                        cd.RelativeColumn(2);
                        cd.RelativeColumn(2);
                        cd.RelativeColumn(2);
                    });

                    t.Header(h =>
                    {
                        h.Cell().Text("Code-name").SemiBold();
                        h.Cell().Text("Name").SemiBold();
                        h.Cell().Text("Sex").SemiBold();
                        h.Cell().Text("DOB").SemiBold();
                        h.Cell().Text("Tier").SemiBold();
                    });

                    foreach (var a in animals)
                    {
                        t.Cell().Text(a.CodeName);
                        t.Cell().Text(a.PrimaryName ?? "—");
                        t.Cell().Text(a.Sex.ToString());
                        t.Cell().Text(a.Dob.ToString("yyyy-MM-dd"));
                        t.Cell().Text(a.PerformanceTier == PerformanceTier.None ? "—" : a.PerformanceTier.ToString());
                    }
                });
                p.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Page ");
                    t.CurrentPageNumber();
                    t.Span(" of ");
                    t.TotalPages();
                });
            });
        });

        return new ReportFile("herd-census.pdf", "application/pdf", doc.GeneratePdf());
    }

    public async Task<ReportFile> HerdCensusExcelAsync(Guid organisationId, CancellationToken ct = default)
    {
        var animals = await db.Animals.AsNoTracking()
            .Where(a => a.OrganisationId == organisationId)
            .OrderBy(a => a.CodeName)
            .ToListAsync(ct);

        using var wb = new XLWorkbook();
        var sheet = wb.AddWorksheet("Herd");
        sheet.Cell(1, 1).Value = "Code-name";
        sheet.Cell(1, 2).Value = "Name";
        sheet.Cell(1, 3).Value = "Sex";
        sheet.Cell(1, 4).Value = "DOB";
        sheet.Cell(1, 5).Value = "Status";
        sheet.Cell(1, 6).Value = "Tier";
        sheet.Cell(1, 7).Value = "B-sired";

        for (int i = 0; i < animals.Count; i++)
        {
            var row = i + 2;
            var a = animals[i];
            sheet.Cell(row, 1).Value = a.CodeName;
            sheet.Cell(row, 2).Value = a.PrimaryName;
            sheet.Cell(row, 3).Value = a.Sex.ToString();
            sheet.Cell(row, 4).Value = a.Dob.ToString("yyyy-MM-dd");
            sheet.Cell(row, 5).Value = a.Status.ToString();
            sheet.Cell(row, 6).Value = a.PerformanceTier.ToString();
            sheet.Cell(row, 7).Value = a.IsBSired ? "Yes" : "No";
        }

        sheet.Range(1, 1, 1, 7).Style.Font.Bold = true;
        sheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return new ReportFile("herd-census.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ms.ToArray());
    }

    public async Task<ReportFile> PerformanceRankingPdfAsync(Guid organisationId, CancellationToken ct = default)
    {
        var ranked = await db.Animals.AsNoTracking()
            .Where(a => a.OrganisationId == organisationId && a.Sex == AnimalSex.Female && a.Status != AnimalStatus.Dead && a.Status != AnimalStatus.Sold)
            .OrderBy(a => a.PerformanceTier)
            .ThenBy(a => a.CodeName)
            .ToListAsync(ct);

        var doc = Document.Create(c =>
        {
            c.Page(p =>
            {
                p.Margin(28);
                p.Size(PageSizes.A4);
                p.Header().Text("Performance ranking").SemiBold().FontSize(14);
                p.Content().Table(t =>
                {
                    t.ColumnsDefinition(cd =>
                    {
                        cd.RelativeColumn(1);
                        cd.RelativeColumn(2);
                        cd.RelativeColumn(3);
                        cd.RelativeColumn(2);
                        cd.RelativeColumn(2);
                    });
                    t.Header(h =>
                    {
                        h.Cell().Text("Tier").SemiBold();
                        h.Cell().Text("Code-name").SemiBold();
                        h.Cell().Text("Name").SemiBold();
                        h.Cell().Text("CPY").SemiBold();
                        h.Cell().Text("Avg interval (d)").SemiBold();
                    });

                    foreach (var a in ranked)
                    {
                        t.Cell().Text(a.PerformanceTier.ToString());
                        t.Cell().Text(a.CodeName);
                        t.Cell().Text(a.PrimaryName ?? "—");
                        t.Cell().Text(a.CalvesPerYear?.ToString("0.00") ?? "—");
                        t.Cell().Text(a.AvgCalvingIntervalDays?.ToString("0") ?? "—");
                    }
                });
            });
        });

        return new ReportFile("performance-ranking.pdf", "application/pdf", doc.GeneratePdf());
    }

    public async Task<ReportFile> CullCandidatesPdfAsync(Guid organisationId, CancellationToken ct = default)
    {
        var cull = await db.Animals.AsNoTracking()
            .Where(a => a.OrganisationId == organisationId && a.PerformanceTier == PerformanceTier.E)
            .OrderBy(a => a.CodeName)
            .ToListAsync(ct);

        var doc = Document.Create(c =>
        {
            c.Page(p =>
            {
                p.Margin(28);
                p.Size(PageSizes.A4);
                p.Header().Text("Cull candidates (Tier E)").SemiBold().FontSize(14);
                p.Content().Column(col =>
                {
                    if (cull.Count == 0)
                    {
                        col.Item().Text("No cows currently flagged for culling.");
                        return;
                    }

                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(cd =>
                        {
                            cd.RelativeColumn(2);
                            cd.RelativeColumn(3);
                            cd.RelativeColumn(2);
                            cd.RelativeColumn(2);
                        });
                        t.Header(h =>
                        {
                            h.Cell().Text("Code-name").SemiBold();
                            h.Cell().Text("Name").SemiBold();
                            h.Cell().Text("DOB").SemiBold();
                            h.Cell().Text("CPY").SemiBold();
                        });
                        foreach (var a in cull)
                        {
                            t.Cell().Text(a.CodeName);
                            t.Cell().Text(a.PrimaryName ?? "—");
                            t.Cell().Text(a.Dob.ToString("yyyy-MM-dd"));
                            t.Cell().Text(a.CalvesPerYear?.ToString("0.00") ?? "—");
                        }
                    });
                });
            });
        });

        return new ReportFile("cull-candidates.pdf", "application/pdf", doc.GeneratePdf());
    }
}
