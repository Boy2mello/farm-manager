using FarmManager.Domain.Common;

namespace FarmManager.Application.Common.Interfaces;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default);
}
