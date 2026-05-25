using FarmManager.Application.Common.Events;
using FarmManager.Application.Common.Interfaces;
using FarmManager.Domain.Common;
using MediatR;

namespace FarmManager.Infrastructure.Notifications;

public sealed class MediatRDomainEventDispatcher(IPublisher publisher) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        foreach (var @event in events)
        {
            await publisher.Publish(new DomainEventEnvelope(@event), ct);
        }
    }
}
