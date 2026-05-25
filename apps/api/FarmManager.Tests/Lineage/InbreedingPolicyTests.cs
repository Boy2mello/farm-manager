using FarmManager.Application.Lineage;
using FluentAssertions;
using Xunit;

namespace FarmManager.Tests.Lineage;

public class InbreedingPolicyTests
{
    [Theory]
    [InlineData(0.0000, InbreedingAction.Allow)]
    [InlineData(0.0156, InbreedingAction.Note)]
    [InlineData(0.0625, InbreedingAction.Warn)]
    [InlineData(0.1250, InbreedingAction.SoftBlock)]
    [InlineData(0.2500, InbreedingAction.HardBlock)]
    public void Spec_thresholds_map_to_actions(decimal f, InbreedingAction expected)
    {
        InbreedingPolicy.Evaluate(f).Action.Should().Be(expected);
    }

    [Fact]
    public void Reason_explains_threshold_breach()
    {
        var v = InbreedingPolicy.Evaluate(0.25m);
        v.Reason.Should().Contain("0.2500");
    }
}
