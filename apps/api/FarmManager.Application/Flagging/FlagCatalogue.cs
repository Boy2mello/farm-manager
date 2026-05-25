using System.Text.Json;
using FarmManager.Domain.Flagging;

namespace FarmManager.Application.Flagging;

public sealed record FlagAssessment(string Code, FlagSeverity Severity, string Reason, string MetricsJson);

/// <summary>
/// Spec §10.3 — cow flag catalogue. Evaluates a cow's metrics and returns every flag that
/// currently applies. The caller compares this list against the persisted flag-set to compute
/// adds and resolves (RULE-017).
/// </summary>
public static class CowFlagCatalogue
{
    public static IReadOnlyList<FlagAssessment> Evaluate(CowPerformanceMetrics m)
    {
        var flags = new List<FlagAssessment>();
        var metrics = JsonSerializer.Serialize(m);

        if (m.Cpy is not null && m.Cpy < 0.5m)
        {
            flags.Add(new FlagAssessment(
                FlagCodes.LowCpy, FlagSeverity.High,
                $"CPY {m.Cpy:0.00} below 0.5 threshold.", metrics));
        }

        if (m.LastCalvingIntervalMonths is > 18m)
        {
            flags.Add(new FlagAssessment(
                FlagCodes.LongCalvingInterval, FlagSeverity.High,
                $"Last calving interval {m.LastCalvingIntervalMonths:0.0} months exceeds 18 months.", metrics));
        }

        if (m.AgeAtFirstCalvingMonths is > 48m)
        {
            flags.Add(new FlagAssessment(
                FlagCodes.LateFirstCalver, FlagSeverity.Medium,
                $"Age at first calf {m.AgeAtFirstCalvingMonths:0.0} months > 48.", metrics));
        }

        if (m.ServicesWithoutConfirmedPregnancy >= 3)
        {
            flags.Add(new FlagAssessment(
                FlagCodes.RepeatBreeder, FlagSeverity.High,
                $"{m.ServicesWithoutConfirmedPregnancy} services without confirmed pregnancy.", metrics));
        }

        if (m.TotalCalves == 0 && m.AgeYears >= 3.5m)
        {
            flags.Add(new FlagAssessment(
                FlagCodes.NeverCalvedOverdue, FlagSeverity.High,
                $"Age {m.AgeYears:0.0} years with no calf.", metrics));
        }

        if (m.CalfMortalityLast30d)
        {
            flags.Add(new FlagAssessment(
                FlagCodes.CalfMortality, FlagSeverity.Medium,
                "A calf has died within 30 days of birth.", metrics));
        }

        if (m.CalfLossesLifetime >= 2)
        {
            flags.Add(new FlagAssessment(
                FlagCodes.MultipleCalfLosses, FlagSeverity.High,
                $"{m.CalfLossesLifetime} calf losses lifetime.", metrics));
        }

        if (m.DystociaLastCalving)
        {
            flags.Add(new FlagAssessment(
                FlagCodes.DystociaHistory, FlagSeverity.Medium,
                "Calving difficulty ≥ 4 on last calving.", metrics));
        }

        return flags;
    }
}
