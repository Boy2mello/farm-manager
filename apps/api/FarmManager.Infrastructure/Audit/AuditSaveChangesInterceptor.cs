using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FarmManager.Application.Common.Interfaces;
using FarmManager.Domain.Audit;
using FarmManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FarmManager.Infrastructure.Audit;

/// <summary>
/// Spec §19 — audit log with a SHA-256 hash chain. Intercepts SaveChanges, captures Added /
/// Modified / Deleted entries (skipping audit rows themselves), writes one
/// <see cref="AuditLogEntry"/> per change with <c>entry_hash = SHA256(previous_hash || canonical_payload)</c>.
/// </summary>
public sealed class AuditSaveChangesInterceptor(ICurrentUser currentUser) : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> resultIn,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not FarmManagerDbContext db)
        {
            return await base.SavingChangesAsync(eventData, resultIn, cancellationToken);
        }

        var entries = db.ChangeTracker
            .Entries()
            .Where(e => e.Entity is not AuditLogEntry
                && (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
            .ToList();

        if (entries.Count == 0) return await base.SavingChangesAsync(eventData, resultIn, cancellationToken);

        // The last hash anchors the chain.
        var lastHash = await db.AuditLog
            .OrderByDescending(a => a.Sequence)
            .Select(a => a.EntryHash)
            .FirstOrDefaultAsync(cancellationToken)
            ?? "0000000000000000000000000000000000000000000000000000000000000000";

        var userId = currentUser.UserId;
        var orgId = currentUser.OrganisationId ?? Guid.Empty;
        var userName = currentUser.UserName;

        foreach (var entry in entries)
        {
            var (entityId, before, after) = SnapshotPayload(entry);
            var action = entry.State switch
            {
                EntityState.Added => "create",
                EntityState.Modified => "update",
                EntityState.Deleted => "delete",
                _ => "unknown",
            };

            var log = AuditLogEntry.Create(
                organisationId: orgId,
                userId: userId,
                userName: userName,
                action: action,
                entityType: entry.Metadata.Name,
                entityId: entityId,
                beforeJson: before,
                afterJson: after);

            var payload = $"{lastHash}|{log.OrganisationId}|{log.Action}|{log.EntityType}|{log.EntityId}|{log.BeforeJson ?? ""}|{log.AfterJson}";
            var nextHash = Sha256Hex(payload);
            log.SetChain(0, lastHash, nextHash); // EF assigns Sequence on insert.

            db.AuditLog.Add(log);
            lastHash = nextHash;
        }

        return await base.SavingChangesAsync(eventData, resultIn, cancellationToken);
    }

    private static (string EntityId, string? BeforeJson, string AfterJson) SnapshotPayload(EntityEntry entry)
    {
        var idValue = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? "?";

        var before = entry.State is EntityState.Modified or EntityState.Deleted
            ? JsonSerializer.Serialize(entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue))
            : null;

        var after = entry.State is EntityState.Added or EntityState.Modified
            ? JsonSerializer.Serialize(entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue))
            : "{}";

        return (idValue, before, after);
    }

    private static string Sha256Hex(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }
}
