namespace FarmManager.Application.Flagging;

/// <summary>
/// Per spec Appendix F. All values are computed from the event stream — never stored as ground truth.
/// </summary>
public sealed record CowPerformanceMetrics(
    Guid AnimalId,
    decimal AgeYears,
    decimal ProductiveYears,
    int TotalCalves,
    int CalvesAlive,
    int CalfLossesLifetime,
    decimal? Cpy,
    decimal? AvgCalvingIntervalMonths,
    decimal? LastCalvingIntervalMonths,
    decimal? MonthsSinceLastCalving,
    decimal? AgeAtFirstCalvingMonths,
    int ServicesWithoutConfirmedPregnancy,
    bool DystociaLastCalving,
    bool CalfMortalityLast30d,
    bool IsFirstTimeMotherThisCycle);
