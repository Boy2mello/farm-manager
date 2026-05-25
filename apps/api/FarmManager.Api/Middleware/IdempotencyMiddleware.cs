using System.Collections.Concurrent;

namespace FarmManager.Api.Middleware;

/// <summary>
/// Honours the <c>Idempotency-Key</c> header on non-GET requests (spec §22.3). A retried request
/// with the same key inside the TTL window short-circuits and replays the cached response body.
/// In-process cache today; Phase D moves it to Redis when MassTransit also lands.
/// </summary>
public sealed class IdempotencyMiddleware(RequestDelegate next)
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(15);
    private static readonly ConcurrentDictionary<string, CachedResponse> Cache = new();

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("Idempotency-Key", out var keyValues)
            || string.IsNullOrWhiteSpace(keyValues.ToString())
            || HttpMethods.IsGet(context.Request.Method)
            || HttpMethods.IsHead(context.Request.Method))
        {
            await next(context);
            return;
        }

        var key = keyValues.ToString();
        EvictExpired();

        if (Cache.TryGetValue(key, out var cached))
        {
            context.Response.StatusCode = cached.StatusCode;
            context.Response.ContentType = cached.ContentType;
            context.Response.Headers["X-Idempotent-Replay"] = "true";
            await context.Response.WriteAsync(cached.Body);
            return;
        }

        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        await next(context);

        buffer.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(buffer).ReadToEndAsync();
        buffer.Seek(0, SeekOrigin.Begin);
        await buffer.CopyToAsync(originalBody);
        context.Response.Body = originalBody;

        if (context.Response.StatusCode is >= 200 and < 300 or 409)
        {
            Cache[key] = new CachedResponse(
                context.Response.StatusCode,
                context.Response.ContentType ?? "application/json",
                responseBody,
                DateTimeOffset.UtcNow.Add(Ttl));
        }
    }

    private static void EvictExpired()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var kvp in Cache)
        {
            if (kvp.Value.ExpiresAt < now)
            {
                Cache.TryRemove(kvp.Key, out _);
            }
        }
    }

    private sealed record CachedResponse(int StatusCode, string ContentType, string Body, DateTimeOffset ExpiresAt);
}
