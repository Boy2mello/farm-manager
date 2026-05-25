namespace FarmManager.Application.Common.Interfaces;

public interface IDateTime
{
    DateTimeOffset UtcNow { get; }
    DateOnly TodayInOrganisation(string ianaTimeZone);
}
