namespace FarmManager.Domain.Audit;

/// <summary>
/// Spec §19 — append-only audit log with a SHA-256 hash chain. Each row's <see cref="EntryHash"/>
/// is computed from the previous row's hash + the canonical JSON of this row's payload, making
/// retroactive tampering detectable.
/// </summary>
public sealed class AuditLogEntry
{
    public long Sequence { get; private set; }
    public Guid Id { get; private set; }
    public Guid OrganisationId { get; private set; }
    public Guid? UserId { get; private set; }
    public string? UserName { get; private set; }
    public string Action { get; private set; } = default!;
    public string EntityType { get; private set; } = default!;
    public string EntityId { get; private set; } = default!;
    public string? BeforeJson { get; private set; }
    public string AfterJson { get; private set; } = "{}";
    public DateTimeOffset OccurredAt { get; private set; }
    public string PreviousHash { get; private set; } = "0000000000000000000000000000000000000000000000000000000000000000";
    public string EntryHash { get; private set; } = default!;

    private AuditLogEntry() { }

    public static AuditLogEntry Create(
        Guid organisationId,
        Guid? userId,
        string? userName,
        string action,
        string entityType,
        string entityId,
        string? beforeJson,
        string afterJson) => new()
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisationId,
            UserId = userId,
            UserName = userName,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            BeforeJson = beforeJson,
            AfterJson = afterJson,
            OccurredAt = DateTimeOffset.UtcNow,
        };

    public void SetChain(long sequence, string previousHash, string entryHash)
    {
        Sequence = sequence;
        PreviousHash = previousHash;
        EntryHash = entryHash;
    }
}
