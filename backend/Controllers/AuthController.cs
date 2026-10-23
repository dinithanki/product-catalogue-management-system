
using System.Security.Claims;
using backend.DTOs.Requests;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(
            request.Username,
            request.Password);

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
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request)
    {
        var result = await authService.RefreshTokenAsync(
            request.RefreshToken);

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
    public async Task<IActionResult> Logout(
        [FromBody] RefreshTokenRequest request)
    {
        var revoked = await authService.LogoutAsync(
            request.RefreshToken);

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
