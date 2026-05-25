using System.Text.Json;
using FarmManager.Application.Notifications;
using FarmManager.Domain.Notifications;
using FarmManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebPush;

namespace FarmManager.Infrastructure.Notifications;

public sealed class WebPushOptions
{
    public string Subject { get; set; } = "mailto:owner@farm-manager.local";
    public string PublicKey { get; set; } = default!;
    public string PrivateKey { get; set; } = default!;
}

public sealed class WebPushNotificationChannel(
    FarmManagerDbContext db,
    IOptions<WebPushOptions> options,
    ILogger<WebPushNotificationChannel> logger) : INotificationChannel
{
    private readonly WebPushOptions _options = options.Value;

    public NotificationChannel Channel => NotificationChannel.WebPush;

    public async Task SendAsync(NotificationRequest request, Guid? recipientUserId, CancellationToken ct = default)
    {
        if (recipientUserId is null) return;
        if (string.IsNullOrWhiteSpace(_options.PublicKey) || string.IsNullOrWhiteSpace(_options.PrivateKey))
        {
            logger.LogWarning("Web Push not configured — skipping push to user {User}", recipientUserId);
            return;
        }

        var subs = await db.Set<PushSubscription>()
            .Where(s => s.UserId == recipientUserId)
            .ToListAsync(ct);

        if (subs.Count == 0) return;

        var vapid = new VapidDetails(_options.Subject, _options.PublicKey, _options.PrivateKey);
        var client = new WebPushClient();

        var payload = JsonSerializer.Serialize(new
        {
            title = request.Title,
            body = request.Body,
            topic = request.Topic,
            data = request.Data,
        });

        foreach (var s in subs)
        {
            try
            {
                var endpoint = new global::WebPush.PushSubscription(s.Endpoint, s.P256dh, s.Auth);
                await client.SendNotificationAsync(endpoint, payload, vapid, ct);
            }
            catch (WebPushException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Gone or System.Net.HttpStatusCode.NotFound)
            {
                db.Set<PushSubscription>().Remove(s);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Web push failed to {Endpoint}", s.Endpoint);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
