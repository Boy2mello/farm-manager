using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Xunit;

namespace FarmManager.Tests.Audit;

/// <summary>
/// Verifies the SHA-256 hash chain semantics that Audit interceptors rely on.
/// </summary>
public class AuditChainTests
{
    [Fact]
    public void Chain_is_deterministic_and_breaks_when_a_row_is_tampered()
    {
        string prev = new string('0', 64);
        var rows = new[] { "row-1", "row-2", "row-3" };
        var hashes = rows.Select(r =>
        {
            prev = Hash($"{prev}|{r}");
            return prev;
        }).ToArray();

        // Recompute with the second row tampered:
        string repPrev = new string('0', 64);
        var tampered = new[] { "row-1", "row-2!", "row-3" };
        var tamperedHashes = tampered.Select(r =>
        {
            repPrev = Hash($"{repPrev}|{r}");
            return repPrev;
        }).ToArray();

        hashes[0].Should().Be(tamperedHashes[0]);
        hashes[1].Should().NotBe(tamperedHashes[1]);
        hashes[2].Should().NotBe(tamperedHashes[2]);
    }

    private static string Hash(string s)
        => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(s)));
}
