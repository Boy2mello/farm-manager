using FarmManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FarmManager.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[EnableRateLimiting("auth")]
public sealed class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IJwtTokenService tokens) : ControllerBase
{
    public sealed record LoginRequest(string Email, string Password);
    public sealed record LoginResponse(string AccessToken, DateTimeOffset AccessExpiresAt, string RefreshToken, DateTimeOffset RefreshExpiresAt);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest body, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(body.Email);
        if (user is null)
        {
            return Unauthorized();
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, body.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            return Unauthorized();
        }

        var roles = await userManager.GetRolesAsync(user);
        var issued = tokens.Issue(user, roles);

        return Ok(new LoginResponse(
            issued.AccessToken,
            issued.AccessExpiresAt,
            issued.RefreshToken,
            issued.RefreshExpiresAt));
    }

    [HttpGet("me")]
    public IActionResult Me() => Ok(new { user = User.Identity?.Name, isAuthenticated = User.Identity?.IsAuthenticated ?? false });
}
