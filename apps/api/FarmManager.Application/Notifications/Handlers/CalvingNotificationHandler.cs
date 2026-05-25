using FarmManager.Application.Common.Events;
using FarmManager.Domain.Breeding;
using FarmManager.Domain.Notifications;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FarmManager.Application.Notifications.Handlers;

/// <summary>
/// Fans out a "Calf {code} registered" message to Owner + Manager when a calving event lands
/// (spec §13.1, §16.2). The dispatcher honours quiet hours; calving is Normal severity so it
/// will queue in the in-app inbox during quiet hours and fire Web Push + WhatsApp otherwise.
/// </summary>
public sealed class CalvingNotificationHandler(
    INotificationService notifications,
    ILogger<CalvingNotificationHandler> logger)
    : INotificationHandler<DomainEventEnvelope>
{
    public async Task Handle(DomainEventEnvelope notification, CancellationToken cancellationToken)
    {
        if (notification.Event is not CalvingRecordedEvent calving) return;

        var headline = calving.Stillbirth
            ? $"Stillbirth recorded for calf {calving.CalfCodeName}"
            : $"Calf {calving.CalfCodeName} registered";

        try
        {
            await notifications.DispatchAsync(new NotificationRequest(
                OrganisationId: calving.OrganisationId,
                UserId: null, // broadcast to org owners + managers
                Topic: "calving.recorded",
                Title: headline,
                Body: calving.Stillbirth
                    ? "Dam needs follow-up. Tap to open the calving event."
                    : "Tap to open the calf profile. Print the ear tag from the action menu.",
                Severity: calving.Stillbirth ? NotificationSeverity.Critical : NotificationSeverity.Normal,
                Data: new Dictionary<string, string>
                {
                    ["calfId"] = calving.CalfId.ToString(),
                    ["damId"] = calving.DamId.ToString(),
                    ["codeName"] = calving.CalfCodeName,
                }), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Calving notification failed for {Code}", calving.CalfCodeName);
        }
    }
}
