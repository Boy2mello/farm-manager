namespace FarmManager.Domain.Common;

/// <summary>
/// A versioned, immutable fact emitted from the domain (e.g. <c>calving.recorded.v1</c>).
/// The Application layer adapts these to MediatR notifications.
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
