namespace FarmManager.Infrastructure.Identity;

public interface IJwtTokenService
{
    JwtTokens Issue(ApplicationUser user, IEnumerable<string> roles);
}

public sealed record JwtTokens(string AccessToken, DateTimeOffset AccessExpiresAt, string RefreshToken, DateTimeOffset RefreshExpiresAt);
