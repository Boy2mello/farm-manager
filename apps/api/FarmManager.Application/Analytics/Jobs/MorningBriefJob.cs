using FarmManager.Application.Common.Interfaces;
using FarmManager.Application.Notifications;
using FarmManager.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmManager.Application.Analytics.Jobs;

/// <summary>
/// 07:00 SAST owner brief (spec §11.18 / §13.4). Composes a one-line summary of the top 3
/// deltas from yesterday's KPI snapshot and dispatches via Push + WhatsApp + Email.
/// </summary>
public sealed class MorningBriefJob(
    IFarmManagerDbContext db,
    INotificationService notifications,
    ILogger<MorningBriefJob> logger)
{
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var orgs = await db.Organisations.Select(o => o.Id).ToListAsync(ct);

        foreach (var orgId in orgs)
        {
            var snapshots = await db.HerdKpiSnapshots.AsNoTracking()
                .Where(s => s.OrganisationId == orgId && s.AsOfDate == today && s.DeltaVsLastYear != null)
                .OrderByDescending(s => Math.Abs(s.DeltaVsLastYear ?? 0))
                .Take(3)
                .ToListAsync(ct);

            if (snapshots.Count == 0)
            {
                logger.LogInformation("MorningBrief: no snapshot for org {Org} on {Day} — skipping", orgId, today);
                continue;
            }

            var body = string.Join("  ·  ", snapshots.Select(s =>
                $"{Pretty(s.MetricName)} {Format(s.Value)} ({DeltaSign(s.DeltaVsLastYear)} YoY)"));

            await notifications.DispatchAsync(new NotificationRequest(
                OrganisationId: orgId,
                UserId: null,
                Topic: "kpi.morning_brief",
                Title: "Morning brief",
                Body: body,
                Severity: NotificationSeverity.Normal,
                Channels: new[] { NotificationChannel.WebPush, NotificationChannel.WhatsApp, NotificationChannel.Email, NotificationChannel.InApp }
            ), ct);
        }
    }

    private static string Pretty(string metric) => metric switch
    {
        "live_cattle" => "Live cattle",
        "confirmed_pregnancies" => "Confirmed pregnancies",
        "calves_ytd" => "Calves YTD",
        "calving_rate" => "Calving rate",
        "mortality_rate_ytd" => "Mortality YTD",
        "avg_calving_interval_months" => "Avg interval",
        "calf_mortality_30d" => "Calf mortality 30d",
        "watch_list" => "Watch list",
        _ => metric,
    };

    private static string Format(decimal value) => value % 1 == 0 ? ((int)value).ToString() : value.ToString("0.00");
    private static string DeltaSign(decimal? d) => d switch
    {
        null => "—",
        > 0 => $"+{d:0.00}",
        < 0 => $"{d:0.00}",
        _ => "0",
    };
}
