using FarmManager.Application.Common.Interfaces;
using FarmManager.Domain.Animals;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmManager.Application.Animals.Queries;

public sealed record AnimalSummary(
    Guid Id,
    string CodeName,
    string? PrimaryName,
    AnimalSex Sex,
    DateOnly Dob,
    AnimalStatus Status,
    PerformanceTier PerformanceTier,
    bool IsBSired);

public sealed record ListAnimalsQuery(string? Search = null, AnimalStatus? Status = null) : IRequest<IReadOnlyList<AnimalSummary>>;

public sealed class ListAnimalsHandler(IFarmManagerDbContext db, ICurrentUser currentUser)
    : IRequestHandler<ListAnimalsQuery, IReadOnlyList<AnimalSummary>>
{
    public async Task<IReadOnlyList<AnimalSummary>> Handle(ListAnimalsQuery request, CancellationToken ct)
    {
        var organisationId = currentUser.OrganisationId
            ?? throw new InvalidOperationException("Current user has no organisation context.");

        var q = db.Animals.AsNoTracking().Where(a => a.OrganisationId == organisationId);
        if (request.Status is { } st) q = q.Where(a => a.Status == st);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim().ToLowerInvariant();
            q = q.Where(a =>
                a.CodeName.ToLower().Contains(s)
                || (a.PrimaryName != null && a.PrimaryName.ToLower().Contains(s)));
        }

        return await q
            .OrderBy(a => a.CodeName)
            .Select(a => new AnimalSummary(
                a.Id, a.CodeName, a.PrimaryName, a.Sex, a.Dob, a.Status, a.PerformanceTier, a.IsBSired))
            .ToListAsync(ct);
    }
}

public sealed record GetAnimalByIdQuery(Guid AnimalId) : IRequest<AnimalDetail?>;

public sealed record AnimalDetail(
    Guid Id,
    string CodeName,
    string? PrimaryName,
    IReadOnlyList<string> Aliases,
    AnimalSex Sex,
    DateOnly Dob,
    DobPrecision DobPrecision,
    AnimalSource Source,
    AnimalStatus Status,
    PerformanceTier PerformanceTier,
    bool IsBSired,
    Guid? DamId,
    Guid? SireId,
    int CalfCount,
    int CalvesAlive,
    decimal? AvgCalvingIntervalDays,
    decimal? CalvesPerYear,
    DateOnly? LastCalvingDate);

public sealed class GetAnimalByIdHandler(IFarmManagerDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetAnimalByIdQuery, AnimalDetail?>
{
    public async Task<AnimalDetail?> Handle(GetAnimalByIdQuery request, CancellationToken ct)
    {
        var organisationId = currentUser.OrganisationId
            ?? throw new InvalidOperationException("Current user has no organisation context.");

        var a = await db.Animals.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.AnimalId && x.OrganisationId == organisationId, ct);
        if (a is null) return null;

        return new AnimalDetail(
            a.Id, a.CodeName, a.PrimaryName, a.Aliases, a.Sex, a.Dob, a.DobPrecision,
            a.Source, a.Status, a.PerformanceTier, a.IsBSired, a.DamId, a.SireId,
            a.CalfCount, a.CalvesAlive, a.AvgCalvingIntervalDays, a.CalvesPerYear, a.LastCalvingDate);
    }
}
