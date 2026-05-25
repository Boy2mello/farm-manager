using FarmManager.Application.Common.Interfaces;
using FarmManager.Domain.Analytics;
using FarmManager.Domain.Animals;
using FarmManager.Domain.Audit;
using FarmManager.Domain.Breeding;
using FarmManager.Domain.Commerce;
using FarmManager.Domain.Common;
using FarmManager.Domain.Flagging;
using FarmManager.Domain.Health;
using FarmManager.Domain.Inventory;
using FarmManager.Domain.Notifications;
using FarmManager.Domain.Organisations;
using FarmManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FarmManager.Infrastructure.Persistence;

public class FarmManagerDbContext(DbContextOptions<FarmManagerDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options), IFarmManagerDbContext
{
    public DbSet<Organisation> Organisations => Set<Organisation>();
    public DbSet<Animal> Animals => Set<Animal>();
    public DbSet<CodeNameSequence> CodeNameSequences => Set<CodeNameSequence>();
    public DbSet<CalvingEvent> CalvingEvents => Set<CalvingEvent>();
    public DbSet<ServiceEvent> ServiceEvents => Set<ServiceEvent>();
    public DbSet<PregnancyCheckEvent> PregnancyCheckEvents => Set<PregnancyCheckEvent>();
    public DbSet<Flag> Flags => Set<Flag>();
    public DbSet<TierAssignment> TierAssignments => Set<TierAssignment>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();
    public DbSet<HerdKpiSnapshot> HerdKpiSnapshots => Set<HerdKpiSnapshot>();
    public DbSet<HealthEvent> HealthEvents => Set<HealthEvent>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<SaleEvent> SaleEvents => Set<SaleEvent>();
    public DbSet<PurchaseEvent> PurchaseEvents => Set<PurchaseEvent>();
    public DbSet<DeathEvent> DeathEvents => Set<DeathEvent>();
    public DbSet<TransferEvent> TransferEvents => Set<TransferEvent>();
    public DbSet<AuditLogEntry> AuditLog => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(FarmManagerDbContext).Assembly);

        // Identity tables: keep ASP.NET defaults; just snake_case the dotnet names.
        foreach (var entity in builder.Model.GetEntityTypes())
        {
            var table = entity.GetTableName();
            if (table is not null && table.StartsWith("AspNet", StringComparison.Ordinal))
            {
                entity.SetTableName(table.ToLowerInvariant());
            }
        }
    }

    /// <summary>
    /// Atomic code-name sequence reservation (RULE-019). Postgres-only: uses an UPSERT with
    /// <c>ON CONFLICT … DO UPDATE</c> and a <c>RETURNING</c> clause so concurrent calvings cannot
    /// produce duplicate sequence numbers.
    /// </summary>
    public async Task<int> ReserveCodeNameSequenceAsync(Guid organisationId, string prefix, int year, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO code_name_sequences (organisation_id, prefix, year, next_sequence)
            VALUES ({0}, {1}, {2}, 2)
            ON CONFLICT (organisation_id, prefix, year)
            DO UPDATE SET next_sequence = code_name_sequences.next_sequence + 1
            RETURNING next_sequence - 1 AS assigned_sequence;
            """;

        await using var conn = (NpgsqlConnection)Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
        {
            await conn.OpenAsync(ct);
        }

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO code_name_sequences (organisation_id, prefix, year, next_sequence)
            VALUES (@org, @prefix, @year, 2)
            ON CONFLICT (organisation_id, prefix, year)
            DO UPDATE SET next_sequence = code_name_sequences.next_sequence + 1
            RETURNING next_sequence - 1;
            """;
        cmd.Parameters.AddWithValue("org", organisationId);
        cmd.Parameters.AddWithValue("prefix", prefix.ToUpperInvariant());
        cmd.Parameters.AddWithValue("year", year);

        var result = await cmd.ExecuteScalarAsync(ct);
        if (result is null or DBNull)
        {
            throw new InvalidOperationException("Failed to reserve code-name sequence.");
        }

        return Convert.ToInt32(result);
    }
}
