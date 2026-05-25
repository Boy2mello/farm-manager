using FarmManager.Application.Flagging;
using FarmManager.Domain.Animals;
using FluentAssertions;
using Xunit;

namespace FarmManager.Tests.Analytics;

/// <summary>
/// Light-weight unit-style checks on the tier matrix that the nightly recalc job depends on.
/// </summary>
public class TierRecalcJobTests
{
    [Fact]
    public void Cow_with_no_calf_at_age_4_lands_in_E()
    {
        var m = new CowPerformanceMetrics(
            AnimalId: Guid.NewGuid(),
            AgeYears: 4.5m,
            ProductiveYears: 2.5m,
            TotalCalves: 0,
            CalvesAlive: 0,
            CalfLossesLifetime: 0,
            Cpy: 0m,
            AvgCalvingIntervalMonths: null,
            LastCalvingIntervalMonths: null,
            MonthsSinceLastCalving: null,
            AgeAtFirstCalvingMonths: null,
            ServicesWithoutConfirmedPregnancy: 0,
            DystociaLastCalving: false,
            CalfMortalityLast30d: false,
            IsFirstTimeMotherThisCycle: false);

        var result = TierEvaluator.Evaluate(m);
        result.Tier.Should().Be(PerformanceTier.E);
        result.Reason.Should().Contain("≥ 3.5");
    }

    [Fact]
    public void High_performing_cow_lands_in_A_with_reason_referencing_metrics()
    {
        var m = new CowPerformanceMetrics(
            AnimalId: Guid.NewGuid(),
            AgeYears: 6m,
            ProductiveYears: 4m,
            TotalCalves: 3,
            CalvesAlive: 3,
            CalfLossesLifetime: 0,
            Cpy: 0.75m,
            AvgCalvingIntervalMonths: 13m,
            LastCalvingIntervalMonths: 13m,
            MonthsSinceLastCalving: 4m,
            AgeAtFirstCalvingMonths: 30m,
            ServicesWithoutConfirmedPregnancy: 0,
            DystociaLastCalving: false,
            CalfMortalityLast30d: false,
            IsFirstTimeMotherThisCycle: false);

        var result = TierEvaluator.Evaluate(m);
        result.Tier.Should().Be(PerformanceTier.A);
        result.Reason.Should().Contain("CPY=0.75");
    }
}
