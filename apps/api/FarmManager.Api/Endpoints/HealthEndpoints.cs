namespace FarmManager.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/healthz", () => Results.Ok(new { status = "ok", at = DateTimeOffset.UtcNow }))
            .WithName("Liveness")
            .WithTags("Health")
            .AllowAnonymous();

        app.MapHealthChecks("/readyz").AllowAnonymous();

        return app;
    }
}
