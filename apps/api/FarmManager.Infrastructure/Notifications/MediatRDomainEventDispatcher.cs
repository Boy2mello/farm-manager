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
            // Domain events stay MediatR-free; the dispatcher adapts them to MediatR notifications
            // by wrapping in a generic envelope so they're addressable by handlers.
            await publisher.Publish(new DomainEventEnvelope(@event), ct);
        }
    }
}

public sealed record DomainEventEnvelope(IDomainEvent Event) : INotification;
