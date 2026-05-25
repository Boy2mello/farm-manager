using System.Net.Http.Json;
using System.Text.Json;
using FarmManager.Application.Notifications;
using FarmManager.Domain.Notifications;
using FarmManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FarmManager.Infrastructure.Notifications;

public sealed class WhatsAppOptions
{
    public string PhoneNumberId { get; set; } = default!;
    public string AccessToken { get; set; } = default!;
    public string ApiBase { get; set; } = "https://graph.facebook.com/v20.0";
    public string DefaultTemplateName { get; set; } = "farm_alert";
    public string DefaultTemplateLanguage { get; set; } = "en";
}

/// <summary>
/// Meta WhatsApp Cloud API direct integration (Appendix H decision). Spec §16.1 / §16.2.
/// Uses pre-approved template messages for alerts; free text for in-session replies.
/// </summary>
public sealed class WhatsAppNotificationChannel(
    FarmManagerDbContext db,
    IHttpClientFactory httpClientFactory,
    IOptions<WhatsAppOptions> options,
    ILogger<WhatsAppNotificationChannel> logger) : INotificationChannel
{
    private readonly WhatsAppOptions _options = options.Value;

    public NotificationChannel Channel => NotificationChannel.WhatsApp;

    public async Task SendAsync(NotificationRequest request, Guid? recipientUserId, CancellationToken ct = default)
    {
        if (recipientUserId is null) return;
        if (string.IsNullOrWhiteSpace(_options.AccessToken) || string.IsNullOrWhiteSpace(_options.PhoneNumberId))
        {
            logger.LogWarning("WhatsApp not configured — skipping send for user {User}", recipientUserId);
            return;
        }

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == recipientUserId, ct);
        if (user?.PhoneNumber is null || !user.PhoneNumberConfirmed) return;

        var http = httpClientFactory.CreateClient(nameof(WhatsAppNotificationChannel));
        var requestUri = $"{_options.ApiBase}/{_options.PhoneNumberId}/messages";

        var body = new
        {
            messaging_product = "whatsapp",
            to = NormalisePhone(user.PhoneNumber),
            type = "template",
            template = new
            {
                name = _options.DefaultTemplateName,
                language = new { code = _options.DefaultTemplateLanguage },
                components = new[]
                {
                    new
                    {
                        type = "body",
                        parameters = new[]
                        {
                            new { type = "text", text = request.Title },
                            new { type = "text", text = request.Body },
                        },
                    },
                },
            },
        };

        using var http_req = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(body),
        };
        http_req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.AccessToken);

        try
        {
            var resp = await http.SendAsync(http_req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var payload = await resp.Content.ReadAsStringAsync(ct);
                logger.LogWarning("WhatsApp send failed: {Status} {Body}", resp.StatusCode, payload);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "WhatsApp send threw for user {User}", recipientUserId);
        }
    }

    private static string NormalisePhone(string raw) => raw.TrimStart('+');
}
