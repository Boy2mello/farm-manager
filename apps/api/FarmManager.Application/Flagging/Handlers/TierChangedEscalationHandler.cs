using FarmManager.Application.Analytics.Jobs;
using FarmManager.Application.Common.Events;
using FarmManager.Application.Common.Interfaces;
using FarmManager.Application.Notifications;
using FarmManager.Domain.Animals;
using FarmManager.Domain.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmManager.Application.Flagging.Handlers;

/// <summary>
/// Spec §10.5 escalation policy. Maps tier transitions onto user-facing notifications and
/// the Sale Candidate queue without coupling the tier engine to notifications.
/// </summary>
public sealed class TierChangedEscalationHandler(
    INotificationService notifications,
    IFarmManagerDbContext db,
    ILogger<TierChangedEscalationHandler> logger)
    : INotificationHandler<DomainEventEnvelope>
{
    public async Task Handle(DomainEventEnvelope notification, CancellationToken cancellationToken)
    {
        if (notification.Event is not TierChangedEvent change) return;

        var animal = await db.Animals.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == change.AnimalId, cancellationToken);
        if (animal is null) return;

        var label = animal.PrimaryName ?? animal.CodeName;

        // B → C and shallower transitions: silent (already logged in TierAssignment).
        if (change.Current <= PerformanceTier.C) return;

        if (change.Current == PerformanceTier.D)
        {
            await notifications.DispatchAsync(new NotificationRequest(
                OrganisationId: change.OrganisationId,
                UserId: null,
                Topic: "tier.downgrade.d",
                Title: $"{label} → Tier D (Watch)",
                Body: change.Reason,
                Severity: NotificationSeverity.Normal,
                Data: new Dictionary<string, string> { ["animalId"] = animal.Id.ToString(), ["tier"] = "D" }
            ), cancellationToken);
            return;
        }

        if (change.Current == PerformanceTier.E)
        {
            // Critical: bypass quiet hours.
            await notifications.DispatchAsync(new NotificationRequest(
                OrganisationId: change.OrganisationId,
                UserId: null,
                Topic: "tier.downgrade.e",
                Title: $"{label} → Tier E (Cull candidate)",
                Body: change.Reason,
                Severity: NotificationSeverity.Critical,
                Channels: new[] { NotificationChannel.WebPush, NotificationChannel.WhatsApp, NotificationChannel.Email, NotificationChannel.InApp },
                Data: new Dictionary<string, string> { ["animalId"] = animal.Id.ToString(), ["tier"] = "E" }
            ), cancellationToken);

            logger.LogInformation("Tier E assigned to {Code} ({Name})", animal.CodeName, label);
        }
    }
}
