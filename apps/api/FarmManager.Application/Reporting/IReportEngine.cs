namespace FarmManager.Application.Reporting;

public sealed record ReportFile(string FileName, string ContentType, byte[] Bytes);

public interface IReportEngine
{
    Task<ReportFile> HerdCensusPdfAsync(Guid organisationId, CancellationToken ct = default);
    Task<ReportFile> HerdCensusExcelAsync(Guid organisationId, CancellationToken ct = default);
    Task<ReportFile> PerformanceRankingPdfAsync(Guid organisationId, CancellationToken ct = default);
    Task<ReportFile> CullCandidatesPdfAsync(Guid organisationId, CancellationToken ct = default);
}
