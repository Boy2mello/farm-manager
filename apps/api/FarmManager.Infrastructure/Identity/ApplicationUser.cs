using Microsoft.AspNetCore.Identity;

namespace FarmManager.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public Guid? OrganisationId { get; set; }
    public string? DisplayName { get; set; }
    public string? PreferredLanguage { get; set; } = "en";
    public TimeOnly QuietHoursStart { get; set; } = new(21, 0);
    public TimeOnly QuietHoursEnd { get; set; } = new(6, 0);
}
