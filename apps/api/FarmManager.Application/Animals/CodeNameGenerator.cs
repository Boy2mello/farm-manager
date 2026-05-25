using FarmManager.Application.Common.Interfaces;
using FarmManager.Domain.Animals;
using FarmManager.Domain.Organisations;
using Microsoft.EntityFrameworkCore;

namespace FarmManager.Application.Animals;

public interface ICodeNameGenerator
{
    Task<CodeName> NextAsync(Guid organisationId, AnimalSource source, int year, CancellationToken ct = default);
}

public sealed class CodeNameGenerator(IFarmManagerDbContext db) : ICodeNameGenerator
{
    public async Task<CodeName> NextAsync(Guid organisationId, AnimalSource source, int year, CancellationToken ct = default)
    {
        var org = await db.Organisations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == organisationId, ct)
            ?? throw new InvalidOperationException($"Organisation {organisationId} not found.");

        var prefix = ResolvePrefix(org, source);
        var sequence = await db.ReserveCodeNameSequenceAsync(organisationId, prefix, year, ct);

        return new CodeName(prefix, year, sequence, org.SequenceWidth);
    }

    private static string ResolvePrefix(Organisation org, AnimalSource source) => source switch
    {
        AnimalSource.BornOnFarm => org.CalfPrefix,
        AnimalSource.Purchased => org.PurchasedPrefix,
        AnimalSource.Inherited => org.PurchasedPrefix,
        AnimalSource.TransferredIn => org.PurchasedPrefix,
        AnimalSource.Legacy => org.LegacyPrefix,
        _ => org.CalfPrefix,
    };
}
