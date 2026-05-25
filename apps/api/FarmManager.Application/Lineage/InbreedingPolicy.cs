using FarmManager.Application.Calvings.Commands.RecordCalving;

namespace FarmManager.Application.Lineage;

public enum InbreedingAction
{
    Allow = 0,
    Note = 1,
    Warn = 2,
    SoftBlock = 3,
    HardBlock = 4,
}

public sealed record InbreedingVerdict(decimal F, InbreedingAction Action, string Reason);

/// <summary>
/// RULE-008 (BLOCKING). Spec §12.3 thresholds.
/// </summary>
public static class InbreedingPolicy
{
    public static InbreedingVerdict Evaluate(decimal f)
    {
        if (f >= LineageConstants.HardBlockThreshold)
        {
            return new InbreedingVerdict(f,
                InbreedingAction.HardBlock,
                $"F = {f:0.0000} ≥ {LineageConstants.HardBlockThreshold:0.0000} — full sibling or parent–offspring equivalent. Mating refused.");
        }

        if (f >= LineageConstants.OverrideThreshold)
        {
            return new InbreedingVerdict(f,
                InbreedingAction.SoftBlock,
                $"F = {f:0.0000} ≥ {LineageConstants.OverrideThreshold:0.0000} — half-sibling or grandparent-grandchild. Owner override required.");
        }

        if (f >= LineageConstants.WarnThreshold)
        {
            return new InbreedingVerdict(f,
                InbreedingAction.Warn,
                $"F = {f:0.0000} ≥ {LineageConstants.WarnThreshold:0.0000} — first cousin range. Warning only.");
        }

        if (f > 0m)
        {
            return new InbreedingVerdict(f, InbreedingAction.Note, $"F = {f:0.0000} — distant relation. Allowed with note.");
        }

        return new InbreedingVerdict(0m, InbreedingAction.Allow, "F = 0 — unrelated. Allowed.");
    }
}
