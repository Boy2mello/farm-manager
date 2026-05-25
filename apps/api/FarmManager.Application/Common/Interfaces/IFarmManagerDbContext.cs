using FarmManager.Domain.Animals;
using FarmManager.Domain.Breeding;
using FarmManager.Domain.Flagging;
using FarmManager.Domain.Organisations;
using Microsoft.EntityFrameworkCore;

namespace FarmManager.Application.Common.Interfaces;

public interface IFarmManagerDbContext
{
    DbSet<Organisation> Organisations { get; }
    DbSet<Animal> Animals { get; }
    DbSet<CodeNameSequence> CodeNameSequences { get; }
    DbSet<CalvingEvent> CalvingEvents { get; }
    DbSet<ServiceEvent> ServiceEvents { get; }
    DbSet<PregnancyCheckEvent> PregnancyCheckEvents { get; }
    DbSet<Flag> Flags { get; }
    DbSet<TierAssignment> TierAssignments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reserves the next code-name sequence atomically for the (org, prefix, year) triple
    /// (RULE-019). Implementation uses <c>SELECT … FOR UPDATE</c> inside the current transaction.
    /// </summary>
    Task<int> ReserveCodeNameSequenceAsync(Guid organisationId, string prefix, int year, CancellationToken ct = default);
}
