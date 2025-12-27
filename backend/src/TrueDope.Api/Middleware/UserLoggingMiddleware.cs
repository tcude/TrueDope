using System.Security.Claims;
using Serilog.Context;

namespace TrueDope.Api.Middleware;

public class UserLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public UserLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // User identity
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userEmail = context.User.FindFirstValue(ClaimTypes.Email);

        // Request correlation (links all logs from a single request)
        var correlationId = context.TraceIdentifier;

        // Client information
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // Request details
        var requestPath = context.Request.Path.ToString();
        var requestMethod = context.Request.Method;

        using (LogContext.PushProperty("UserId", userId ?? "anonymous"))
        using (LogContext.PushProperty("UserEmail", userEmail ?? "anonymous"))
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("ClientIp", clientIp))
        using (LogContext.PushProperty("RequestPath", requestPath))
        using (LogContext.PushProperty("RequestMethod", requestMethod))
        {
            await _next(context);
        }
    }
}

public static class UserLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseUserLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<UserLoggingMiddleware>();
    }
}
