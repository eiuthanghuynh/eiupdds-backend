using System.Net;
using System.Security.Claims;
using System.Text.Json;
using eiupdds_backend.Services.Interfaces;

namespace eiupdds_backend.Middlewares;

public class ActiveUserMiddleware
{
    private readonly RequestDelegate _next;

    public ActiveUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUserActiveGuard userActiveGuard)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId) || !await userActiveGuard.IsUserActiveAsync(userId))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Response.ContentType = "application/json";

                var response = new
                {
                    statusCode = (int)HttpStatusCode.Unauthorized,
                    message = "User account has been deactivated or does not exist.",
                    timestamp = DateTime.UtcNow
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                return;
            }
        }

        await _next(context);
    }
}
