using FarmManager.Domain.Animals;
using FluentAssertions;
using Xunit;

namespace FarmManager.Tests.Domain;

public class CodeNameTests
{
    [Theory]
    [InlineData("T", 2026, 24, "T-2026-024")]
    [InlineData("C", 2026, 1, "C-2026-001")]
    [InlineData("L", 2026, 40, "L-2026-040")]
    public void Pads_sequence_to_three_digits(string prefix, int year, int seq, string expected)
    {
        new CodeName(prefix, year, seq).Value.Should().Be(expected);
    }

    [Fact]
    public void Auto_widens_when_sequence_exceeds_default_width()
    {
        new CodeName("T", 2026, 1024).Value.Should().Be("T-2026-1024");
    }

    [Theory]
    [InlineData("T-2026-024", "T", 2026, 24)]
    [InlineData("c-2026-001", "C", 2026, 1)]
    public void Parses_well_formed_code_names(string input, string prefix, int year, int seq)
    {
        var code = CodeName.Parse(input);
        code.Prefix.Should().Be(prefix);
        code.Year.Should().Be(year);
        code.Sequence.Should().Be(seq);
    }

    [Theory]
    [InlineData("")]
    [InlineData("oops")]
    [InlineData("T-26-001")]
    [InlineData("T-2026-99")]
    public void Rejects_malformed_code_names(string input)
    {
        Action act = () => CodeName.Parse(input);
        act.Should().Throw<Exception>();
    }
}
