using System.Text.Json;
using FarmManager.Domain.Animals;

namespace FarmManager.Application.Flagging;

public sealed record TierResult(PerformanceTier Tier, string Reason, string MetricsJson);

/// <summary>
/// Spec §10.4. Rules are evaluated top-down: the first match wins.
/// Every breeding cow lands in exactly one tier, with an explanatory <see cref="TierResult.Reason"/>.
/// First-time mothers (RULE-016) are exempt and return <see cref="PerformanceTier.None"/>.
/// </summary>
public static class TierEvaluator
{
    public static TierResult Evaluate(CowPerformanceMetrics m)
    {
        if (m.IsFirstTimeMotherThisCycle)
        {
            return new TierResult(
                PerformanceTier.None,
                "First-time mother: tier deferred until second calving (RULE-016).",
                SerializeMetrics(m));
        }

        // E — Cull candidate
        if (m.Cpy is < 0.25m)
        {
            return Result(PerformanceTier.E,
                $"CPY {m.Cpy:0.00} < 0.25 — cull recommended.", m);
        }
        if (m.TotalCalves == 0 && m.AgeYears >= 3.5m)
        {
            return Result(PerformanceTier.E,
                $"No calf at age {m.AgeYears:0.0} years (≥ 3.5) — cull recommended.", m);
        }
        if (m.CalfLossesLifetime >= 2)
        {
            return Result(PerformanceTier.E,
                $"Lost {m.CalfLossesLifetime} calves lifetime — cull recommended.", m);
        }

        // D — Watch
        if (m.Cpy is >= 0.25m and < 0.4m
            || m.AvgCalvingIntervalMonths is >= 18m and <= 24m
            || m.AgeAtFirstCalvingMonths is > 48m)
        {
            return Result(PerformanceTier.D,
                BuildReason(m, ">= D thresholds"), m);
        }

        // C — Average
        if (m.Cpy is >= 0.4m and < 0.5m
            || m.AvgCalvingIntervalMonths is >= 16m and < 18m)
        {
            return Result(PerformanceTier.C, BuildReason(m, "in C band"), m);
        }

        // B — Good
        if (m.Cpy is >= 0.5m && m.AvgCalvingIntervalMonths is null or <= 16m)
        {
            return Result(PerformanceTier.B, BuildReason(m, "Good performer"), m);
        }

        // A — Top performer (all-must-be-true)
        if (m.Cpy is >= 0.75m
            && m.AgeAtFirstCalvingMonths is not null and <= 36m
            && !m.CalfMortalityLast30d
            && m.AvgCalvingIntervalMonths is not null and <= 14m)
        {
            return Result(PerformanceTier.A, BuildReason(m, "Top performer"), m);
        }

        return Result(PerformanceTier.C, "Default: insufficient data — monitor.", m);
    }

    private static TierResult Result(PerformanceTier tier, string reason, CowPerformanceMetrics m) =>
        new(tier, reason, SerializeMetrics(m));

    private static string BuildReason(CowPerformanceMetrics m, string headline)
    {
        var bits = new List<string> { headline };
        if (m.Cpy is not null) bits.Add($"CPY={m.Cpy:0.00}");
        if (m.AvgCalvingIntervalMonths is not null) bits.Add($"avg interval={m.AvgCalvingIntervalMonths:0.0}mo");
        if (m.AgeAtFirstCalvingMonths is not null) bits.Add($"age@first calf={m.AgeAtFirstCalvingMonths:0.0}mo");
        return string.Join(" · ", bits);
    }

    private static string SerializeMetrics(CowPerformanceMetrics m) =>
        JsonSerializer.Serialize(m);
}
