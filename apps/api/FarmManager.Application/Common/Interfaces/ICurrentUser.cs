namespace FarmManager.Application.Common.Interfaces;

public interface ICurrentUser
{
    Guid? UserId { get; }
    Guid? OrganisationId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
