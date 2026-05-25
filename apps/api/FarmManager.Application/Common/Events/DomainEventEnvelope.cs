using FarmManager.Domain.Common;
using MediatR;

namespace FarmManager.Application.Common.Events;

/// <summary>
/// MediatR adapter for domain events. Domain emits MediatR-free <see cref="IDomainEvent"/>s;
/// the dispatcher wraps each one in this envelope so it can be picked up by
/// <see cref="INotificationHandler{TNotification}"/>s.
/// </summary>
public sealed record DomainEventEnvelope(IDomainEvent Event) : INotification;
