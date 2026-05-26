using FarmManager.Domain.Common;

namespace FarmManager.Domain.Organisations;

/// <summary>
/// A sub-herd / location within an Organisation. Spec §18.1 lists Farm as a first-class entity
/// so multi-farm filters (Jijo / Olly / Tumi) work end-to-end.
/// </summary>
public sealed class Farm : AggregateRoot<Guid>
{
    public Guid OrganisationId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? OwnerName { get; private set; }
    public string? Notes { get; private set; }

    private Farm() { }

    public static Farm Create(Guid organisationId, string name, string? ownerName = null, string? notes = null)
    {
        if (organisationId == Guid.Empty)
        {
            throw new ArgumentException("OrganisationId is required.", nameof(organisationId));
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Farm name is required.", nameof(name));
        }

        return new Farm
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisationId,
            Name = name.Trim(),
            OwnerName = ownerName?.Trim(),
            Notes = notes?.Trim(),
        };
    }
}
