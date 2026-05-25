using FarmManager.Domain.Notifications;

namespace FarmManager.Application.Notifications;

public sealed record NotificationRequest(
    Guid OrganisationId,
    Guid? UserId,
    string Topic,
    string Title,
    string Body,
    NotificationSeverity Severity = NotificationSeverity.Normal,
    IReadOnlyDictionary<string, string>? Data = null,
    /// <summary>
    /// If null the user's enabled channels are used. If specified, only these channels are tried.
    /// Critical alerts always include Web Push + WhatsApp regardless of quiet hours.
    /// </summary>
    IReadOnlyList<NotificationChannel>? Channels = null);

public interface INotificationService
{
    Task DispatchAsync(NotificationRequest request, CancellationToken ct = default);
}

public interface INotificationChannel
{
    NotificationChannel Channel { get; }
    Task SendAsync(NotificationRequest request, Guid? recipientUserId, CancellationToken ct = default);
}
