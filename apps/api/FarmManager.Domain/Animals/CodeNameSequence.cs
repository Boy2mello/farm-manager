namespace FarmManager.Domain.Animals;

/// <summary>
/// Per-(organisation, prefix, year) sequence row. Atomic reservation via
/// <c>SELECT … FOR UPDATE</c> in the repository (spec §11.19 / §18.1).
/// </summary>
public sealed class CodeNameSequence
{
    public Guid OrganisationId { get; private set; }
    public string Prefix { get; private set; } = default!;
    public int Year { get; private set; }
    public int NextSequence { get; private set; }

    private CodeNameSequence() { }

    public static CodeNameSequence StartOf(Guid organisationId, string prefix, int year) =>
        new()
        {
            OrganisationId = organisationId,
            Prefix = prefix.Trim().ToUpperInvariant(),
            Year = year,
            NextSequence = 1,
        };

    public int Reserve()
    {
        var assigned = NextSequence;
        NextSequence++;
        return assigned;
    }
}
