using FarmManager.Infrastructure.Identity;
using Hangfire.Dashboard;

namespace FarmManager.Api.Middleware;

public sealed class HangfireAdminAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true
            && (httpContext.User.IsInRole(Roles.SuperAdmin) || httpContext.User.IsInRole(Roles.Owner));
    }
}
