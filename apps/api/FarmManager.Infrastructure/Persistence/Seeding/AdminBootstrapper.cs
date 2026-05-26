using FarmManager.Domain.Organisations;
using FarmManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FarmManager.Infrastructure.Persistence.Seeding;

/// <summary>
/// Idempotent bootstrap of the platform's roles and one admin user. The user details and password
/// can be overridden via configuration; defaults are tuned for first-run convenience and MUST be
/// rotated once the deployment is live.
/// </summary>
public static class AdminBootstrapper
{
    public const string DefaultAdminUserName = "Boy2mello";
    public const string DefaultAdminEmail = "Boy2mello@farm-manager.local";
    public const string DefaultAdminPassword = "Boy2mello!Farm26";
    public const string OrganisationName = "Tumi's Farm";

    public static async Task BootstrapAsync(
        FarmManagerDbContext db,
        UserManager<ApplicationUser> users,
        RoleManager<ApplicationRole> roles,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken ct = default)
    {
        // ---------- 1. Roles ----------
        var rolesNeeded = new[]
        {
            Roles.SuperAdmin, Roles.Owner, Roles.FarmManager,
            Roles.Vet, Roles.FieldWorker, Roles.Observer,
        };
        foreach (var role in rolesNeeded)
        {
            if (!await roles.RoleExistsAsync(role))
            {
                await roles.CreateAsync(new ApplicationRole(role));
                logger.LogInformation("Bootstrap: created role {Role}", role);
            }
        }

        // ---------- 2. Organisation ----------
        var org = await db.Organisations.FirstOrDefaultAsync(o => o.Name == OrganisationName, ct);
        if (org is null)
        {
            org = Organisation.Create(OrganisationName);
            db.Organisations.Add(org);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Bootstrap: created organisation {Org}", org.Name);
        }

        // ---------- 3. Admin user ----------
        var userName = configuration["BootstrapAdmin:UserName"] ?? DefaultAdminUserName;
        var email = configuration["BootstrapAdmin:Email"] ?? DefaultAdminEmail;
        var password = configuration["BootstrapAdmin:Password"] ?? DefaultAdminPassword;

        var existing = await users.FindByNameAsync(userName);
        if (existing is null)
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = userName,
                Email = email,
                EmailConfirmed = true,
                OrganisationId = org.Id,
                DisplayName = "Boy2mello (bootstrap admin)",
            };

            var result = await users.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
                throw new InvalidOperationException($"Failed to create bootstrap admin '{userName}': {errors}");
            }

            await users.AddToRolesAsync(user, new[] { Roles.SuperAdmin, Roles.Owner });
            logger.LogWarning(
                "Bootstrap: created admin user '{User}' with password '{Password}'. CHANGE THIS PASSWORD ON FIRST LOGIN.",
                userName, password);
        }
        else
        {
            // Ensure the existing user is still in the admin roles even after manual edits.
            if (!await users.IsInRoleAsync(existing, Roles.SuperAdmin))
            {
                await users.AddToRoleAsync(existing, Roles.SuperAdmin);
            }
            if (!await users.IsInRoleAsync(existing, Roles.Owner))
            {
                await users.AddToRoleAsync(existing, Roles.Owner);
            }
            if (existing.OrganisationId is null || existing.OrganisationId == Guid.Empty)
            {
                existing.OrganisationId = org.Id;
                await users.UpdateAsync(existing);
            }
        }
    }
}
