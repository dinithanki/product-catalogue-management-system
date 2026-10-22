
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    // GET: /api/users
    // Returns all users to an authenticated administrator.
    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var adminId = GetAuthenticatedUserId();

        if (adminId is null)
            return Unauthorized();

        var users = await userService.GetAllUsersAsync(
            adminId.Value);

        return Ok(users);
    }

    // POST: /api/users
    // Creates a user with a backend-generated temporary password.
    [HttpPost]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequest request)
    {
        var adminId = GetAuthenticatedUserId();

        if (adminId is null)
            return Unauthorized();

        var (statusCode, userId, temporaryPassword) =
            await userService.CreateUserAsync(
                adminId.Value,
                request.Username,
                request.Role);

        return statusCode switch
        {
            0 => StatusCode(StatusCodes.Status201Created, new
            {
                message = "User created successfully.",
                userId,
                temporaryPassword,
                mustChangePassword = true
            }),

            1 => BadRequest(new
            {
                message = "Invalid user details."
            }),

            2 => Conflict(new
            {
                message = "Username already exists."
            }),

            4 => StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Administrator access is required."
            }),

            _ => StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Unable to create the user."
            })
        };
    }

    // PUT: /api/users/{userId}
    // Updates a user's username and role.
    [HttpPut("{userId:int}")]
    public async Task<IActionResult> UpdateUser(
        int userId,
        [FromBody] UpdateUserRequest request)
    {
        var adminId = GetAuthenticatedUserId();

        if (adminId is null)
            return Unauthorized();

        var statusCode = await userService.UpdateUserAsync(
            adminId.Value,
            userId,
            request.Username,
            request.Role);

        return statusCode switch
        {
            0 => Ok(new
            {
                message = "User updated successfully."
            }),

            1 => BadRequest(new
            {
                message = "Invalid user details."
            }),

            2 => NotFound(new
            {
                message = "User not found."
            }),

            3 => Conflict(new
            {
                message = "The last active administrator cannot be demoted."
            }),

            4 => StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Administrator access is required."
            }),

            _ => StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Unable to update the user."
            })
        };
    }

    // POST: /api/users/{userId}/reset-password
    // Generates a new temporary password without requiring a request body.
    [HttpPost("{userId:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int userId)
    {
        var adminId = GetAuthenticatedUserId();

        if (adminId is null)
            return Unauthorized();

        var (statusCode, temporaryPassword) =
            await userService.ResetUserPasswordAsync(
                adminId.Value,
                userId);

        return statusCode switch
        {
            0 => Ok(new
            {
                message = "Password reset successfully.",
                temporaryPassword,
                mustChangePassword = true
            }),

            1 => BadRequest(new
            {
                message = "Invalid user ID."
            }),

            2 => NotFound(new
            {
                message = "User not found."
            }),

            4 => StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Administrator access is required."
            }),

            _ => StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Unable to reset the password."
            })
        };
    }

    // PATCH: /api/users/{userId}/activate
    [HttpPatch("{userId:int}/activate")]
    public async Task<IActionResult> ActivateUser(int userId)
    {
        var adminId = GetAuthenticatedUserId();

        if (adminId is null)
            return Unauthorized();

        var statusCode = await userService.ActivateUserAsync(
            adminId.Value,
            userId);

        return statusCode switch
        {
            0 => Ok(new
            {
                message = "User activated successfully."
            }),

            2 => NotFound(new
            {
                message = "User not found."
            }),

            4 => StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Administrator access is required."
            }),

            _ => StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Unable to activate the user."
            })
        };
    }

    // PATCH: /api/users/{userId}/deactivate
    [HttpPatch("{userId:int}/deactivate")]
    public async Task<IActionResult> DeactivateUser(int userId)
    {
        var adminId = GetAuthenticatedUserId();

        if (adminId is null)
            return Unauthorized();

        var statusCode = await userService.DeactivateUserAsync(
            adminId.Value,
            userId);

        return statusCode switch
        {
            0 => Ok(new
            {
                message = "User deactivated successfully."
            }),

            2 => NotFound(new
            {
                message = "User not found."
            }),

            3 => Conflict(new
            {
                message = "The last active administrator cannot be deactivated."
            }),

            4 => StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Administrator access is required."
            }),

            5 => BadRequest(new
            {
                message = "You cannot deactivate your own account."
            }),

            _ => StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Unable to deactivate the user."
            })
        };
    }

    private int? GetAuthenticatedUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(value, out var userId)
            ? userId
            : null;
    }
}

// Request DTOs

public sealed record CreateUserRequest(
    [Required]
    [StringLength(50, MinimumLength = 3)]
    string Username,

    [Required]
    [RegularExpression("^(Admin|User)$")]
    string Role);

public sealed record UpdateUserRequest(
    [Required]
    [StringLength(50, MinimumLength = 3)]
    string Username,

    [Required]
    [RegularExpression("^(Admin|User)$")]
    string Role);
