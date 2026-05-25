namespace FarmManager.Application.Lineage;

/// <summary>
/// Spec §12.2 pedigree query API. Implemented with Postgres recursive CTEs (Dapper).
/// </summary>
public interface IPedigreeQueries
{
    Task<IReadOnlyList<PedigreeNode>> GetAncestorsAsync(Guid animalId, int generations = 4, CancellationToken ct = default);
    Task<IReadOnlyList<PedigreeNode>> GetDescendantsAsync(Guid animalId, int generations = 4, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetFullSiblingsAsync(Guid animalId, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetHalfSiblingsAsync(Guid animalId, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> CommonAncestorsAsync(Guid a, Guid b, int generations = 6, CancellationToken ct = default);
}
