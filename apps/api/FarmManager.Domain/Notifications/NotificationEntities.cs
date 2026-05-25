namespace FarmManager.Domain.Notifications;

public enum NotificationChannel
{
    InApp = 1,
    WebPush = 2,
    WhatsApp = 3,
    Email = 4,
    Sms = 5,
}

public enum NotificationSeverity
{
    Info = 0,
    Normal = 1,
    Critical = 2,
}

public sealed class PushSubscription
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Endpoint { get; private set; } = default!;
    public string P256dh { get; private set; } = default!;
    public string Auth { get; private set; } = default!;
    public string? UserAgent { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private PushSubscription() { }

    public static PushSubscription Create(Guid userId, string endpoint, string p256dh, string auth, string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(endpoint)) throw new ArgumentException("endpoint is required.", nameof(endpoint));
        if (string.IsNullOrWhiteSpace(p256dh)) throw new ArgumentException("p256dh is required.", nameof(p256dh));
        if (string.IsNullOrWhiteSpace(auth)) throw new ArgumentException("auth is required.", nameof(auth));

        return new PushSubscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Endpoint = endpoint,
            P256dh = p256dh,
            Auth = auth,
            UserAgent = userAgent,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}

public sealed class NotificationDelivery
{
    public Guid Id { get; private set; }
    public Guid OrganisationId { get; private set; }
    public Guid? UserId { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public NotificationSeverity Severity { get; private set; }
    public string Topic { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public string PayloadJson { get; private set; } = "{}";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public string? Error { get; private set; }
    public int Attempts { get; private set; }

    private NotificationDelivery() { }

    public static NotificationDelivery Pending(
        Guid organisationId,
        Guid? userId,
        NotificationChannel channel,
        NotificationSeverity severity,
        string topic,
        string title,
        string body,
        string payloadJson)
        => new()
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisationId,
            UserId = userId,
            Channel = channel,
            Severity = severity,
            Topic = topic,
            Title = title,
            Body = body,
            PayloadJson = payloadJson,
            CreatedAt = DateTimeOffset.UtcNow,
        };

    public void MarkDelivered() { DeliveredAt = DateTimeOffset.UtcNow; Error = null; }
    public void MarkFailed(string error) { Error = error; Attempts++; }
}
