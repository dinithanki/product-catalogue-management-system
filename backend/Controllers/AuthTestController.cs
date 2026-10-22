
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/auth-test")]
[Authorize]
public sealed class AuthTestController : ControllerBase
{
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        return Ok(new
        {
            UserId = User.FindFirstValue(
                ClaimTypes.NameIdentifier),

            Username = User.Identity?.Name,

            Role = User.FindFirstValue(
                ClaimTypes.Role)
        });
    }
}
