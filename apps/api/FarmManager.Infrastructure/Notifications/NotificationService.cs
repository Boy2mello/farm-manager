using FarmManager.Application.Common.Interfaces;
using FarmManager.Application.Notifications;
using FarmManager.Domain.Notifications;
using FarmManager.Infrastructure.Identity;
using FarmManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmManager.Infrastructure.Notifications;

public sealed class NotificationService(
    FarmManagerDbContext db,
    IEnumerable<INotificationChannel> channels,
    ILogger<NotificationService> logger,
    IFarmManagerDbContext _) : INotificationService
{
    public async Task DispatchAsync(NotificationRequest request, CancellationToken ct = default)
    {
        var recipientIds = await ResolveRecipientsAsync(request, ct);

        foreach (var userId in recipientIds)
        {
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is null) continue;

            var allowed = SelectChannels(request, user);

            foreach (var ch in allowed)
            {
                var sender = channels.FirstOrDefault(c => c.Channel == ch);
                if (sender is null) continue;

                try
                {
                    await sender.SendAsync(request, userId, ct);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Notification {Channel} failed for user {User}", ch, userId);
                }
            }
        }
    }

    /// <summary>
    /// If the caller specified channels, honour them verbatim — minus quiet-hours suppression
    /// unless this is a critical alert. Otherwise the user's default set is Push + WhatsApp.
    /// </summary>
    private static IReadOnlyList<NotificationChannel> SelectChannels(NotificationRequest request, ApplicationUser user)
    {
        var requested = request.Channels ?? new[] { NotificationChannel.WebPush, NotificationChannel.WhatsApp };
        if (request.Severity == NotificationSeverity.Critical)
        {
            return requested;
        }

        if (IsInQuietHours(user))
        {
            return new[] { NotificationChannel.InApp }; // Only the in-app inbox during quiet hours.
        }

        return requested;
    }

    private static bool IsInQuietHours(ApplicationUser user)
    {
        var now = TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(2)); // Default SAST until per-user TZ lands.
        var start = user.QuietHoursStart;
        var end = user.QuietHoursEnd;
        return start < end ? now >= start && now < end : now >= start || now < end;
    }

    /// <summary>
    /// MVP recipient resolution: explicit user wins; else broadcast to org owners.
    /// </summary>
    private async Task<IReadOnlyList<Guid>> ResolveRecipientsAsync(NotificationRequest request, CancellationToken ct)
    {
        if (request.UserId is { } u) return new[] { u };

        return await db.Users.AsNoTracking()
            .Where(x => x.OrganisationId == request.OrganisationId)
            .Select(x => x.Id)
            .ToListAsync(ct);
    }
}
