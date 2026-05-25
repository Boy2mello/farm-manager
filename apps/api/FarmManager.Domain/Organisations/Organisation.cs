using FarmManager.Domain.Common;

namespace FarmManager.Domain.Organisations;

public sealed class Organisation : AggregateRoot<Guid>
{
    public string Name { get; private set; } = default!;
    public string TimeZone { get; private set; } = "Africa/Johannesburg";

    // Code-name template configuration (spec §8.1.1).
    public string CalfPrefix { get; private set; } = "C";
    public string PurchasedPrefix { get; private set; } = "P";
    public string LegacyPrefix { get; private set; } = "L";
    public int SequenceWidth { get; private set; } = 3;

    private Organisation() { }

    public static Organisation Create(string name, string? timeZone = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Organisation name is required.", nameof(name));
        }

        return new Organisation
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            TimeZone = timeZone ?? "Africa/Johannesburg",
        };
    }
}
