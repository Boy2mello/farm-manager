using Hangfire;

namespace FarmManager.Application.Analytics.Jobs;

public static class HangfireScheduling
{
    public const string TierRecalcId = "nightly-tier-recalc";
    public const string KpiSnapshotId = "nightly-kpi-snapshot";
    public const string MorningBriefId = "morning-brief";

    public static void RegisterFarmJobs(IRecurringJobManager jobs)
    {
        // 02:00 SAST (UTC+2) → 00:00 UTC; Hangfire cron is in server TZ.
        jobs.AddOrUpdate<NightlyTierRecalcJob>(
            TierRecalcId,
            "analytics",
            j => j.ExecuteAsync(CancellationToken.None),
            "0 0 * * *");

        jobs.AddOrUpdate<NightlyKpiSnapshotJob>(
            KpiSnapshotId,
            "analytics",
            j => j.ExecuteAsync(CancellationToken.None),
            "10 0 * * *");

        jobs.AddOrUpdate<MorningBriefJob>(
            MorningBriefId,
            "notifications",
            j => j.ExecuteAsync(CancellationToken.None),
            "0 5 * * *"); // 05:00 UTC = 07:00 SAST.
    }
}
