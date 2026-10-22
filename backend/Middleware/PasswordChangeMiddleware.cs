
using System.Security.Claims;
using backend.Repositories;

namespace backend.Middleware;

public sealed class PasswordChangeMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        AuthRepository authRepository)
    {
        var path = context.Request.Path;

        // Allow login and password change to reach their controllers.
        // The password-change controller still requires authentication.
        var isLoginEndpoint = path.Equals(
            "/api/auth/login",
            StringComparison.OrdinalIgnoreCase);

        var isChangePasswordEndpoint = path.Equals(
            "/api/auth/change-password",
            StringComparison.OrdinalIgnoreCase);

        if (context.User.Identity?.IsAuthenticated == true
            && !isLoginEndpoint
            && !isChangePasswordEndpoint)
        {
            var userIdValue = context.User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out var userId))
            {
                context.Response.StatusCode =
                    StatusCodes.Status403Forbidden;

                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Unable to verify the authenticated user."
                });

                return;
            }

            // Read the latest user status through the existing
            // Dapper repository and SQL stored procedure.
            var user = await authRepository.GetUserByIdAsync(userId);

            if (user is null || !user.IsActive)
            {
                context.Response.StatusCode =
                    StatusCodes.Status403Forbidden;

                await context.Response.WriteAsJsonAsync(new
                {
                    message = "The user account is unavailable."
                });

                return;
            }

            if (user.MustChangePassword)
            {
                context.Response.StatusCode =
                    StatusCodes.Status403Forbidden;

                await context.Response.WriteAsJsonAsync(new
                {
                    message =
                        "You must change your password before accessing this resource."
                });

                return;
            }
        }

        await next(context);
    }
}
