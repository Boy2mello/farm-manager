using System.Text.RegularExpressions;

namespace FarmManager.Domain.Animals;

/// <summary>
/// The immutable identifier issued to every animal at registration, per spec §8.1.1.
/// Format: <c>&lt;PREFIX&gt;-&lt;YYYY&gt;-&lt;NNN&gt;</c>, e.g. <c>T-2026-024</c>.
/// </summary>
public readonly record struct CodeName
{
    private static readonly Regex Pattern =
        new(@"^(?<prefix>[A-Z]{1,8})-(?<year>\d{4})-(?<seq>\d{3,6})$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public string Prefix { get; }
    public int Year { get; }
    public int Sequence { get; }
    public string Value { get; }

    public CodeName(string prefix, int year, int sequence, int width = 3)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new ArgumentException("Prefix is required.", nameof(prefix));
        }

        prefix = prefix.Trim().ToUpperInvariant();
        if (!Regex.IsMatch(prefix, "^[A-Z]{1,8}$"))
        {
            throw new ArgumentException("Prefix must be 1–8 ASCII letters.", nameof(prefix));
        }

        if (year is < 1900 or > 2999)
        {
            throw new ArgumentOutOfRangeException(nameof(year), "Year must be between 1900 and 2999.");
        }

        if (sequence < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be ≥ 1.");
        }

        // Auto-widen if the sequence exceeds the configured width (spec §8.1.1 rule).
        var effectiveWidth = Math.Max(width, sequence.ToString().Length);

        Prefix = prefix;
        Year = year;
        Sequence = sequence;
        Value = $"{prefix}-{year:D4}-{sequence.ToString().PadLeft(effectiveWidth, '0')}";
    }

    public static CodeName Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Code-name is required.", nameof(value));
        }

        var match = Pattern.Match(value.Trim().ToUpperInvariant());
        if (!match.Success)
        {
            throw new FormatException($"Invalid code-name: '{value}'. Expected <PREFIX>-<YYYY>-<NNN>.");
        }

        var seqStr = match.Groups["seq"].Value;
        return new CodeName(
            prefix: match.Groups["prefix"].Value,
            year: int.Parse(match.Groups["year"].Value),
            sequence: int.Parse(seqStr),
            width: seqStr.Length);
    }

    public override string ToString() => Value;
    public static implicit operator string(CodeName c) => c.Value;
}
