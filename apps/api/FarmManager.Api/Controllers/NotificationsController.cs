using FarmManager.Application.Common.Interfaces;
using FarmManager.Domain.Notifications;
using FarmManager.Infrastructure.Notifications;
using FarmManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FarmManager.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public sealed class NotificationsController(
    FarmManagerDbContext db,
    ICurrentUser currentUser,
    IOptions<WebPushOptions> webPush) : ControllerBase
{
    public sealed record SubscribeRequest(string Endpoint, string P256dh, string Auth, string? UserAgent);

    [HttpGet("push/public-key")]
    [AllowAnonymous]
    public IActionResult GetPublicKey() => Ok(new { publicKey = webPush.Value.PublicKey });

    [HttpPost("push/subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest body, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new InvalidOperationException("No user context.");

        var existing = await db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == body.Endpoint, ct);
        if (existing is not null) return NoContent();

        var sub = PushSubscription.Create(userId, body.Endpoint, body.P256dh, body.Auth, body.UserAgent);
        db.PushSubscriptions.Add(sub);
        await db.SaveChangesAsync(ct);
        return Created($"/api/v1/push/subscribe/{sub.Id}", new { id = sub.Id });
    }

    [HttpDelete("push/subscribe")]
    public async Task<IActionResult> Unsubscribe([FromQuery] string endpoint, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new InvalidOperationException("No user context.");
        var subs = await db.PushSubscriptions
            .Where(s => s.UserId == userId && s.Endpoint == endpoint)
            .ToListAsync(ct);
        db.PushSubscriptions.RemoveRange(subs);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    public sealed record AlertRow(Guid Id, string Topic, string Title, string Body, int Severity, DateTimeOffset CreatedAt, DateTimeOffset? DeliveredAt);

    [HttpGet("alerts")]
    public async Task<IActionResult> Alerts([FromQuery] bool? unread, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var orgId = currentUser.OrganisationId;

        var q = db.NotificationDeliveries.AsNoTracking()
            .Where(n => n.OrganisationId == orgId
                && (n.UserId == userId || n.UserId == null)
                && n.Channel == NotificationChannel.InApp);

        if (unread == true) q = q.Where(n => n.DeliveredAt == null);

        var rows = await q
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .Select(n => new AlertRow(n.Id, n.Topic, n.Title, n.Body, (int)n.Severity, n.CreatedAt, n.DeliveredAt))
            .ToListAsync(ct);

        return Ok(rows);
    }
}
