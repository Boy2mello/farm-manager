using System.Security.Claims;
using FarmManager.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace FarmManager.Infrastructure.Identity;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public Guid? UserId =>
        TryParseGuid(Principal?.FindFirstValue(ClaimTypes.NameIdentifier));

    public Guid? OrganisationId =>
        TryParseGuid(Principal?.FindFirstValue("org_id"));

    public string? UserName => Principal?.Identity?.Name;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public bool IsInRole(string role) => Principal?.IsInRole(role) == true;

    private static Guid? TryParseGuid(string? value) =>
        Guid.TryParse(value, out var id) ? id : null;
}
