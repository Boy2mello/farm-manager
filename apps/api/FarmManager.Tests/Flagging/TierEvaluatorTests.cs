using FarmManager.Application.Flagging;
using FarmManager.Domain.Animals;
using FluentAssertions;
using Xunit;

namespace FarmManager.Tests.Flagging;

public class TierEvaluatorTests
{
    private static CowPerformanceMetrics M(
        decimal age = 6m,
        decimal? cpy = null,
        decimal? avgIntervalMonths = null,
        decimal? lastIntervalMonths = null,
        decimal? ageAtFirstCalvingMonths = null,
        int calfLosses = 0,
        bool firstTime = false) =>
        new(
            AnimalId: Guid.NewGuid(),
            AgeYears: age,
            ProductiveYears: Math.Max(0, age - 2),
            TotalCalves: cpy is null ? 0 : (int)Math.Round((decimal)cpy * (age - 2)),
            CalvesAlive: 0,
            CalfLossesLifetime: calfLosses,
            Cpy: cpy,
            AvgCalvingIntervalMonths: avgIntervalMonths,
            LastCalvingIntervalMonths: lastIntervalMonths,
            MonthsSinceLastCalving: null,
            AgeAtFirstCalvingMonths: ageAtFirstCalvingMonths,
            ServicesWithoutConfirmedPregnancy: 0,
            DystociaLastCalving: false,
            CalfMortalityLast30d: false,
            IsFirstTimeMotherThisCycle: firstTime);

    [Fact]
    public void Top_performers_land_in_tier_A()
    {
        var m = M(cpy: 0.85m, avgIntervalMonths: 13m, ageAtFirstCalvingMonths: 30m);
        TierEvaluator.Evaluate(m).Tier.Should().Be(PerformanceTier.A);
    }

    [Fact]
    public void Cull_threshold_low_cpy_is_E()
    {
        var m = M(cpy: 0.2m);
        TierEvaluator.Evaluate(m).Tier.Should().Be(PerformanceTier.E);
    }

    [Fact]
    public void Never_calved_at_age_4_is_E()
    {
        var m = M(age: 4m, cpy: null);
        TierEvaluator.Evaluate(m).Tier.Should().Be(PerformanceTier.E);
    }

    [Fact]
    public void Two_calf_losses_is_E()
    {
        var m = M(cpy: 0.6m, avgIntervalMonths: 14m, calfLosses: 2);
        TierEvaluator.Evaluate(m).Tier.Should().Be(PerformanceTier.E);
    }

    [Fact]
    public void Long_interval_lands_in_D()
    {
        var m = M(cpy: 0.45m, avgIntervalMonths: 20m);
        TierEvaluator.Evaluate(m).Tier.Should().Be(PerformanceTier.D);
    }

    [Fact]
    public void First_time_mother_is_exempt()
    {
        var m = M(firstTime: true);
        TierEvaluator.Evaluate(m).Tier.Should().Be(PerformanceTier.None);
    }

    [Fact]
    public void Every_result_carries_a_reason()
    {
        var m = M(cpy: 0.6m, avgIntervalMonths: 14m);
        var r = TierEvaluator.Evaluate(m);
        r.Reason.Should().NotBeNullOrWhiteSpace();
        r.MetricsJson.Should().Contain("Cpy");
    }
}
