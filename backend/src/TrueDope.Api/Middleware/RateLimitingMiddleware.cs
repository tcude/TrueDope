using System.Collections.Concurrent;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using TrueDope.Api.DTOs;

namespace TrueDope.Api.Middleware;

/// <summary>
/// Rate limiting middleware with different limits for auth endpoints (per-IP) and API endpoints (per-user)
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitingOptions _options;

    // In-memory storage for rate limiting (consider Redis for multi-instance deployments)
    private static readonly ConcurrentDictionary<string, RateLimitEntry> _rateLimits = new();

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, RateLimitingOptions options)
    {
        _next = next;
        _logger = logger;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        var method = context.Request.Method;

        // Determine rate limit configuration based on endpoint
        var (key, limit, windowSeconds) = GetRateLimitConfig(context, path, method);

        // Skip rate limiting if no key (shouldn't happen, but safety check)
        if (string.IsNullOrEmpty(key))
        {
            await _next(context);
            return;
        }

        // Check and update rate limit
        var now = DateTime.UtcNow;
        var entry = _rateLimits.AddOrUpdate(
            key,
            _ => new RateLimitEntry { Count = 1, WindowStart = now },
            (_, existing) =>
            {
                // Reset window if expired
                if (now - existing.WindowStart > TimeSpan.FromSeconds(windowSeconds))
                {
                    return new RateLimitEntry { Count = 1, WindowStart = now };
                }

                // Increment count
                existing.Count++;
                return existing;
            });

        // Check if rate limited
        if (entry.Count > limit)
        {
            var retryAfter = (int)(windowSeconds - (now - entry.WindowStart).TotalSeconds);
            retryAfter = Math.Max(1, retryAfter);

            _logger.LogWarning(
                "Rate limit exceeded for {Key} on {Path}. Count: {Count}, Limit: {Limit}",
                key, path, entry.Count, limit);

            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.ContentType = "application/json";
            context.Response.Headers["Retry-After"] = retryAfter.ToString();

            var errorResponse = new ApiErrorResponse
            {
                Success = false,
                Message = "Too many requests. Please try again later.",
                Error = new ApiError
                {
                    Code = "RATE_LIMIT_EXCEEDED",
                    Description = $"You have exceeded the rate limit. Please wait {retryAfter} seconds before trying again."
                }
            };

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, options));
            return;
        }

        // Add rate limit headers
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, limit - entry.Count).ToString();
            context.Response.Headers["X-RateLimit-Reset"] = ((DateTimeOffset)(entry.WindowStart.AddSeconds(windowSeconds))).ToUnixTimeSeconds().ToString();
            return Task.CompletedTask;
        });

        await _next(context);
    }

    private (string key, int limit, int windowSeconds) GetRateLimitConfig(HttpContext context, string path, string method)
    {
        var clientIp = GetClientIpAddress(context);

        // Auth endpoints - rate limit by IP
        if (path.StartsWith("/api/auth/"))
        {
            // Login endpoint - stricter limit
            if (path == "/api/auth/login" && method == "POST")
            {
                return ($"login:{clientIp}", _options.LoginAttemptsPerMinute, 60);
            }

            // Registration - very strict limit
            if (path == "/api/auth/register" && method == "POST")
            {
                return ($"register:{clientIp}", _options.RegistrationsPerHour, 3600);
            }

            // Password reset request - strict limit
            if (path == "/api/auth/forgot-password" && method == "POST")
            {
                return ($"forgot-password:{clientIp}", _options.PasswordResetsPerHour, 3600);
            }

            // Other auth endpoints - standard limit by IP
            return ($"auth:{clientIp}", _options.AuthRequestsPerMinute, 60);
        }

        // API endpoints - rate limit by user if authenticated, otherwise by IP
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrEmpty(userId))
        {
            return ($"api:user:{userId}", _options.ApiRequestsPerMinutePerUser, 60);
        }

        // Unauthenticated API requests - by IP
        return ($"api:ip:{clientIp}", _options.ApiRequestsPerMinutePerIp, 60);
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP (behind proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // Take the first IP in the chain (original client)
            return forwardedFor.Split(',')[0].Trim();
        }

        // Fall back to direct connection IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    // Cleanup old entries periodically (call from background service if needed)
    public static void CleanupExpiredEntries(int maxAgeSeconds = 3600)
    {
        var now = DateTime.UtcNow;
        var keysToRemove = _rateLimits
            .Where(kvp => now - kvp.Value.WindowStart > TimeSpan.FromSeconds(maxAgeSeconds))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _rateLimits.TryRemove(key, out _);
        }
    }

    private class RateLimitEntry
    {
        public int Count { get; set; }
        public DateTime WindowStart { get; set; }
    }
}

/// <summary>
/// Configuration options for rate limiting
/// </summary>
public class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Login attempts per minute per IP (default: 5)</summary>
    public int LoginAttemptsPerMinute { get; set; } = 5;

    /// <summary>Registrations per hour per IP (default: 3)</summary>
    public int RegistrationsPerHour { get; set; } = 3;

    /// <summary>Password reset requests per hour per email/IP (default: 3)</summary>
    public int PasswordResetsPerHour { get; set; } = 3;

    /// <summary>Auth endpoint requests per minute per IP (default: 20)</summary>
    public int AuthRequestsPerMinute { get; set; } = 20;

    /// <summary>API requests per minute per authenticated user (default: 100)</summary>
    public int ApiRequestsPerMinutePerUser { get; set; } = 100;

    /// <summary>API requests per minute per IP for unauthenticated requests (default: 30)</summary>
    public int ApiRequestsPerMinutePerIp { get; set; } = 30;
}

/// <summary>
/// Extension methods for rate limiting middleware
/// </summary>
public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app, RateLimitingOptions? options = null)
    {
        options ??= new RateLimitingOptions();
        return app.UseMiddleware<RateLimitingMiddleware>(options);
    }

    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app, Action<RateLimitingOptions> configureOptions)
    {
        var options = new RateLimitingOptions();
        configureOptions(options);
        return app.UseMiddleware<RateLimitingMiddleware>(options);
    }
}
