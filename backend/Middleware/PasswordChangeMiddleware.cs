
using System.Security.Claims;
using backend.Repositories;

namespace backend.Middleware;

public sealed class PasswordChangeMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        IAuthRepository authRepository)
    {
        var path = context.Request.Path;

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
