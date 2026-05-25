using FarmManager.Domain.Organisations;
using FluentAssertions;
using Xunit;

namespace FarmManager.Tests;

public class SmokeTests
{
    [Fact]
    public void Organisation_Create_assigns_a_new_id_and_defaults()
    {
        var org = Organisation.Create("Tumi's Farm");

        org.Id.Should().NotBe(Guid.Empty);
        org.Name.Should().Be("Tumi's Farm");
        org.TimeZone.Should().Be("Africa/Johannesburg");
        org.CalfPrefix.Should().Be("C");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Organisation_Create_rejects_blank_names(string blank)
    {
        var act = () => Organisation.Create(blank);
        act.Should().Throw<ArgumentException>();
    }
}
