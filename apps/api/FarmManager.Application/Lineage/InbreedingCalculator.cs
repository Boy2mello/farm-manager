using FarmManager.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FarmManager.Application.Lineage;

/// <summary>
/// Wright's inbreeding coefficient (spec §12.3):
/// <code>
/// F(offspring) = Σ over common ancestors C of:
///   (1/2)^(n_A + n_B + 1) × (1 + F_C)
/// </code>
/// where <c>n_A</c> and <c>n_B</c> are the number of links from each parent to the common ancestor.
/// </summary>
public interface IInbreedingCalculator
{
    /// <summary>
    /// Computes the F coefficient of the (hypothetical) offspring of <paramref name="sireId"/> × <paramref name="damId"/>.
    /// </summary>
    Task<decimal> ComputeAsync(Guid sireId, Guid damId, int maxGenerations = 6, CancellationToken ct = default);
}

public sealed class InbreedingCalculator(IFarmManagerDbContext db) : IInbreedingCalculator
{
    public async Task<decimal> ComputeAsync(Guid sireId, Guid damId, int maxGenerations = 6, CancellationToken ct = default)
    {
        if (sireId == damId)
        {
            return 0.5m; // Self-mating: theoretical only.
        }

        var sireAncestors = await CollectAncestorsAsync(sireId, maxGenerations, ct);
        var damAncestors = await CollectAncestorsAsync(damId, maxGenerations, ct);

        // Common ancestors with shortest path lengths from each parent.
        decimal f = 0m;
        foreach (var (ancestorId, sireSteps) in sireAncestors)
        {
            if (!damAncestors.TryGetValue(ancestorId, out var damSteps))
            {
                continue;
            }

            // Ignore the trivial "the animal is its own ancestor" case.
            if (sireSteps == 0 || damSteps == 0)
            {
                continue;
            }

            // F_ancestor is assumed 0 unless we extend to multi-tier pedigrees. Phase 3 will recurse.
            var contribution = (decimal)Math.Pow(0.5, sireSteps + damSteps + 1);
            f += contribution;
        }

        return Math.Round(f, 6);
    }

    private async Task<Dictionary<Guid, int>> CollectAncestorsAsync(Guid root, int maxGenerations, CancellationToken ct)
    {
        var byId = new Dictionary<Guid, int> { [root] = 0 };

        // BFS via repeated parent lookups. Cheap enough at MVP herd size;
        // Phase B.4 swaps this for a single recursive CTE through IPedigreeQueries when scale demands.
        var frontier = new Queue<(Guid id, int depth)>();
        frontier.Enqueue((root, 0));

        while (frontier.Count > 0)
        {
            var (current, depth) = frontier.Dequeue();
            if (depth >= maxGenerations)
            {
                continue;
            }

            var parents = await db.Animals
                .AsNoTracking()
                .Where(a => a.Id == current)
                .Select(a => new { a.DamId, a.SireId })
                .FirstOrDefaultAsync(ct);

            if (parents is null)
            {
                continue;
            }

            foreach (var parent in new[] { parents.DamId, parents.SireId })
            {
                if (parent is null) continue;
                var pid = parent.Value;
                if (!byId.TryGetValue(pid, out var existing) || existing > depth + 1)
                {
                    byId[pid] = depth + 1;
                    frontier.Enqueue((pid, depth + 1));
                }
            }
        }

        return byId;
    }
}
