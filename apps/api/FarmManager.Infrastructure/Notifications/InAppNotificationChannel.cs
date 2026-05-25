using System.Text.Json;
using FarmManager.Application.Notifications;
using FarmManager.Domain.Notifications;
using FarmManager.Infrastructure.Persistence;

namespace FarmManager.Infrastructure.Notifications;

public sealed class InAppNotificationChannel(FarmManagerDbContext db) : INotificationChannel
{
    public NotificationChannel Channel => NotificationChannel.InApp;

    public async Task SendAsync(NotificationRequest request, Guid? recipientUserId, CancellationToken ct = default)
    {
        var record = NotificationDelivery.Pending(
            organisationId: request.OrganisationId,
            userId: recipientUserId,
            channel: NotificationChannel.InApp,
            severity: request.Severity,
            topic: request.Topic,
            title: request.Title,
            body: request.Body,
            payloadJson: JsonSerializer.Serialize(request.Data ?? new Dictionary<string, string>()));

        record.MarkDelivered(); // The in-app inbox just persists; clients poll /api/v1/alerts.
        db.Set<NotificationDelivery>().Add(record);
        await db.SaveChangesAsync(ct);
    }
}
