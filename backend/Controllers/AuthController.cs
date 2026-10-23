
using System.Security.Claims;
using backend.DTOs.Requests;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService, IConfiguration configuration) : ControllerBase
{
    private const string RefreshCookieName = "keells-refresh";
    private const string RefreshCookiePath = "/api/auth";

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request)
    {
        if (!HasAllowedOrigin()) return Forbid();

        var result = await authService.LoginAsync(request.Username, request.Password, SetRefreshCookie);

        if (result is null)
        {
            return Unauthorized(new
            {
                message = "Invalid username or password."
            });
        }

        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh()
    {
        if (!HasAllowedOrigin()) return Forbid();
        var refreshToken = Request.Cookies[RefreshCookieName];
        var result = await authService.RefreshTokenAsync(refreshToken ?? string.Empty, SetRefreshCookie);

        if (result is null)
        {
            return Unauthorized(new
            {
                message = "Invalid, expired, or revoked refresh token."
            });
        }

        return Ok(result);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        if (!HasAllowedOrigin()) return Forbid();
        var refreshToken = Request.Cookies[RefreshCookieName];
        var revoked = await authService.LogoutAsync(refreshToken ?? string.Empty);
        ExpireRefreshCookie();

        if (!revoked)
        {
            return Unauthorized(new
            {
                message = "Invalid or unavailable refresh token."
            });
        }

        return Ok(new
        {
            message = "Logged out successfully."
        });
    }

    private void SetRefreshCookie(string token, DateTime expiresAtUtc)
    {
        Response.Cookies.Append(RefreshCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = RefreshCookiePath,
            Expires = new DateTimeOffset(DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc)),
            IsEssential = true
        });
    }

    private void ExpireRefreshCookie() => Response.Cookies.Delete(RefreshCookieName, new CookieOptions
    {
        HttpOnly = true,
        Secure = Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        Path = RefreshCookiePath
    });

    private bool HasAllowedOrigin()
    {
        if (!Request.Headers.TryGetValue("Origin", out var origin)) return true;
        var allowedOrigins = configuration.GetSection("Cors:FrontendOrigins").Get<string[]>()
            ?? ["http://localhost:5173"];
        return allowedOrigins.Contains(origin.ToString(), StringComparer.OrdinalIgnoreCase);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request)
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
            return Unauthorized();

        try
        {
            var changed = await authService.ChangePasswordAsync(
                userId,
                request.CurrentPassword,
                request.NewPassword);

            if (!changed)
            {
                return BadRequest(new
                {
                    message =
                        "Current password is incorrect or the account is unavailable."
                });
            }

            return Ok(new
            {
                message = "Password changed successfully."
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
