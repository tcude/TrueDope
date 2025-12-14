namespace TrueDope.Api.Middleware;

/// <summary>
/// Middleware to add security headers to all responses
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers before passing to next middleware
        // Setting headers directly ensures they're available for testing
        AddSecurityHeaders(context);

        await _next(context);
    }

    internal static void AddSecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Prevent MIME type sniffing
        if (!headers.ContainsKey("X-Content-Type-Options"))
        {
            headers["X-Content-Type-Options"] = "nosniff";
        }

        // Prevent clickjacking
        if (!headers.ContainsKey("X-Frame-Options"))
        {
            headers["X-Frame-Options"] = "DENY";
        }

        // XSS protection (legacy but still useful for older browsers)
        if (!headers.ContainsKey("X-XSS-Protection"))
        {
            headers["X-XSS-Protection"] = "1; mode=block";
        }

        // Control referrer information
        if (!headers.ContainsKey("Referrer-Policy"))
        {
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        }

        // Prevent browsers from caching sensitive data
        // Only for API responses, not for static assets
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            if (!headers.ContainsKey("Cache-Control"))
            {
                headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            }

            if (!headers.ContainsKey("Pragma"))
            {
                headers["Pragma"] = "no-cache";
            }
        }

        // Content Security Policy (basic policy for API)
        // More restrictive CSP can be added for HTML responses
        if (!headers.ContainsKey("Content-Security-Policy"))
        {
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
        }

        // Permissions Policy (formerly Feature-Policy)
        if (!headers.ContainsKey("Permissions-Policy"))
        {
            headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
        }
    }
}

/// <summary>
/// Extension methods for security headers middleware
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
