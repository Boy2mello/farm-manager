using FarmManager.Application.Common.Interfaces;
using FarmManager.Domain.Animals;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmManager.Application.Lineage.Queries;

/// <summary>
/// Returns the ancestor tree of an animal, flat list with a <c>Generation</c> column.
/// Generation 0 is the root; 1 = parents; 2 = grandparents; …
/// The client uses the (DamId, SireId) on each node to draw the edges.
/// Spec §12.5 — feeds the React Flow pedigree.
/// </summary>
public sealed record GetPedigreeTreeQuery(Guid AnimalId, int Generations = 3)
    : IRequest<PedigreeTreeDto>;

public sealed record PedigreeTreeDto(
    Guid RootId,
    string RootCodeName,
    string? RootPrimaryName,
    int Generations,
    IReadOnlyList<PedigreeNodeDto> Nodes);

public sealed record PedigreeNodeDto(
    Guid AnimalId,
    string CodeName,
    string? PrimaryName,
    int Sex,
    int Status,
    int PerformanceTier,
    bool IsBSired,
    DateOnly Dob,
    Guid? DamId,
    Guid? SireId,
    int Generation);

public sealed class GetPedigreeTreeHandler(
    IFarmManagerDbContext db,
    ICurrentUser currentUser) : IRequestHandler<GetPedigreeTreeQuery, PedigreeTreeDto>
{
    public async Task<PedigreeTreeDto> Handle(GetPedigreeTreeQuery request, CancellationToken ct)
    {
        var orgId = currentUser.OrganisationId
            ?? throw new InvalidOperationException("No organisation context.");

        var maxGen = Math.Clamp(request.Generations, 1, 6);

        // BFS up the parent chain — visit each ancestor at most once and tag with its
        // shortest distance from the root.
        var rootAnimal = await db.Animals.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AnimalId && a.OrganisationId == orgId, ct)
            ?? throw new InvalidOperationException($"Animal {request.AnimalId} not found.");

        var nodes = new Dictionary<Guid, PedigreeNodeDto>();
        var frontier = new Queue<(Guid id, int gen)>();
        frontier.Enqueue((rootAnimal.Id, 0));

        while (frontier.Count > 0)
        {
            var (id, gen) = frontier.Dequeue();
            if (nodes.ContainsKey(id)) continue;

            var animal = await db.Animals.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id, ct);
            if (animal is null) continue;

            nodes[id] = new PedigreeNodeDto(
                AnimalId: animal.Id,
                CodeName: animal.CodeName,
                PrimaryName: animal.PrimaryName,
                Sex: (int)animal.Sex,
                Status: (int)animal.Status,
                PerformanceTier: (int)animal.PerformanceTier,
                IsBSired: animal.IsBSired,
                Dob: animal.Dob,
                DamId: animal.DamId,
                SireId: animal.SireId,
                Generation: gen);

            if (gen >= maxGen) continue;
            if (animal.DamId is { } dam) frontier.Enqueue((dam, gen + 1));
            if (animal.SireId is { } sire) frontier.Enqueue((sire, gen + 1));
        }

        return new PedigreeTreeDto(
            RootId: rootAnimal.Id,
            RootCodeName: rootAnimal.CodeName,
            RootPrimaryName: rootAnimal.PrimaryName,
            Generations: maxGen,
            Nodes: nodes.Values.OrderBy(n => n.Generation).ToList());
    }
}
