using Microsoft.AspNetCore.Identity;

namespace FarmManager.Infrastructure.Identity;

public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() { }
    public ApplicationRole(string name) : base(name) { }
}

public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Owner = "Owner";
    public const string FarmManager = "FarmManager";
    public const string Vet = "Vet";
    public const string FieldWorker = "FieldWorker";
    public const string Observer = "Observer";
    // Bookkeeper deferred to Phase E per Appendix H decision.
}
