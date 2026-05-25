namespace FarmManager.Application.Lineage;

public sealed record PedigreeNode(
    Guid AnimalId,
    Guid? DamId,
    Guid? SireId,
    int Generation,
    string CodeName,
    string? PrimaryName);

public sealed record Relationship(string Kind, int? PaternalSteps, int? MaternalSteps);
