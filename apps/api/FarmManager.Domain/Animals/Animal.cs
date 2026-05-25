using FarmManager.Domain.Common;

namespace FarmManager.Domain.Animals;

/// <summary>
/// Aggregate root for any individual head of livestock. Schema per spec §18.1.
/// </summary>
public sealed class Animal : AggregateRoot<Guid>
{
    public Guid OrganisationId { get; private set; }
    public Guid? FarmId { get; private set; }

    // Code-name — immutable, indexed, unique within org (spec §8.1.1).
    public string CodeName { get; private set; } = default!;
    public string CodeNamePrefix { get; private set; } = default!;
    public int CodeNameYear { get; private set; }
    public int CodeNameSequence { get; private set; }

    public string? PrimaryName { get; private set; }
    public IReadOnlyList<string> Aliases => _aliases.AsReadOnly();
    private readonly List<string> _aliases = new();

    public string? ExternalTag { get; private set; }
    public string? Rfid { get; private set; }

    public AnimalSex Sex { get; private set; }
    public Guid? BreedId { get; private set; }
    public string BreedCompositionJson { get; private set; } = "{}";

    public DateOnly Dob { get; private set; }
    public DobPrecision DobPrecision { get; private set; } = DobPrecision.Day;

    public Guid? SireId { get; private set; }
    public Guid? DamId { get; private set; }

    public AnimalSource Source { get; private set; } = AnimalSource.BornOnFarm;
    public AnimalStatus Status { get; private set; } = AnimalStatus.Active;
    public DateOnly? DisposalDate { get; private set; }
    public string? DisposalReason { get; private set; }

    public Guid? LocationId { get; private set; }

    public IReadOnlyList<string> PhotoUrls => _photoUrls.AsReadOnly();
    private readonly List<string> _photoUrls = new();

    public bool IsBSired { get; private set; }
    public DateOnly? WithdrawalUntil { get; private set; }
    public PerformanceTier PerformanceTier { get; private set; } = PerformanceTier.None;

    // ----- Computed (denormalised; recomputed by RULE-007) -----
    public int CalfCount { get; private set; }
    public int CalvesAlive { get; private set; }
    public DateOnly? LastCalvingDate { get; private set; }
    public decimal? AvgCalvingIntervalDays { get; private set; }
    public decimal? CalvesPerYear { get; private set; }

    private Animal() { }

    public static Animal Register(
        Guid organisationId,
        CodeName codeName,
        AnimalSex sex,
        DateOnly dob,
        DobPrecision dobPrecision,
        AnimalSource source,
        Guid? damId = null,
        Guid? sireId = null,
        Guid? breedId = null,
        Guid? farmId = null,
        string? primaryName = null,
        IEnumerable<string>? aliases = null)
    {
        if (organisationId == Guid.Empty)
        {
            throw new ArgumentException("OrganisationId is required.", nameof(organisationId));
        }

        var animal = new Animal
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisationId,
            FarmId = farmId,
            CodeName = codeName.Value,
            CodeNamePrefix = codeName.Prefix,
            CodeNameYear = codeName.Year,
            CodeNameSequence = codeName.Sequence,
            PrimaryName = primaryName?.Trim(),
            Sex = sex,
            BreedId = breedId,
            Dob = dob,
            DobPrecision = dobPrecision,
            SireId = sireId,
            DamId = damId,
            Source = source,
            Status = AnimalStatus.Active,
        };

        if (aliases is not null)
        {
            foreach (var alias in aliases.Where(a => !string.IsNullOrWhiteSpace(a)))
            {
                animal._aliases.Add(alias.Trim());
            }
        }

        animal.Raise(new AnimalRegisteredEvent(animal.Id, animal.OrganisationId, animal.CodeName, DateTimeOffset.UtcNow));
        return animal;
    }

    public void Rename(string? newPrimaryName, IEnumerable<string>? newAliases = null)
    {
        // Code-name is immutable — only the given name can change.
        PrimaryName = newPrimaryName?.Trim();
        if (newAliases is not null)
        {
            _aliases.Clear();
            _aliases.AddRange(newAliases.Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Trim()));
        }
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsBSired()
    {
        if (!IsBSired)
        {
            IsBSired = true;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public void TransitionTo(AnimalStatus next)
    {
        Status = next;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetLastCalving(DateOnly when)
    {
        LastCalvingDate = when;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdatePerformance(int calfCount, int calvesAlive, decimal? avgIntervalDays, decimal? cpy, PerformanceTier tier)
    {
        CalfCount = calfCount;
        CalvesAlive = calvesAlive;
        AvgCalvingIntervalDays = avgIntervalDays;
        CalvesPerYear = cpy;
        PerformanceTier = tier;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AddPhoto(string url)
    {
        if (!string.IsNullOrWhiteSpace(url) && !_photoUrls.Contains(url))
        {
            _photoUrls.Add(url);
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public void SetWithdrawal(DateOnly until)
    {
        WithdrawalUntil = WithdrawalUntil is null || until > WithdrawalUntil ? until : WithdrawalUntil;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public sealed record AnimalRegisteredEvent(
    Guid AnimalId,
    Guid OrganisationId,
    string CodeName,
    DateTimeOffset OccurredAt) : IDomainEvent;
