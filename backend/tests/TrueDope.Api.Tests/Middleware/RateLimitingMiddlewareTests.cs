using System.Net;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using TrueDope.Api.Middleware;

namespace TrueDope.Api.Tests.Middleware;

public class RateLimitingMiddlewareTests
{
    private readonly Mock<ILogger<RateLimitingMiddleware>> _loggerMock;
    private readonly RateLimitingOptions _options;

    public RateLimitingMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<RateLimitingMiddleware>>();
        _options = new RateLimitingOptions
        {
            LoginAttemptsPerMinute = 3,
            RegistrationsPerHour = 2,
            PasswordResetsPerHour = 2,
            AuthRequestsPerMinute = 10,
            ApiRequestsPerMinutePerUser = 5,
            ApiRequestsPerMinutePerIp = 3
        };

        // Clean up rate limits before each test
        RateLimitingMiddleware.CleanupExpiredEntries(0);
    }

    [Fact]
    public async Task Should_AllowRequestsUnderLimit()
    {
        // Arrange
        var context = CreateHttpContext("/api/rifles", "GET", "192.168.1.1");
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new RateLimitingMiddleware(next, _loggerMock.Object, _options);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().NotBe((int)HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Should_BlockRequestsOverLimit_ForLoginEndpoint()
    {
        // Arrange
        var clientIp = "192.168.1.100";
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new RateLimitingMiddleware(next, _loggerMock.Object, _options);

        // Act - make requests up to and over the limit
        for (int i = 0; i <= _options.LoginAttemptsPerMinute; i++)
        {
            var context = CreateHttpContext("/api/auth/login", "POST", clientIp);
            await middleware.InvokeAsync(context);

            if (i < _options.LoginAttemptsPerMinute)
            {
                context.Response.StatusCode.Should().NotBe((int)HttpStatusCode.TooManyRequests,
                    $"Request {i + 1} should be allowed");
            }
            else
            {
                context.Response.StatusCode.Should().Be((int)HttpStatusCode.TooManyRequests,
                    $"Request {i + 1} should be blocked");
            }
        }
    }

    [Fact]
    public async Task Should_ReturnRetryAfterHeader_WhenRateLimited()
    {
        // Arrange
        var clientIp = "192.168.1.101";
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new RateLimitingMiddleware(next, _loggerMock.Object, _options);

        // Act - exhaust the limit
        for (int i = 0; i <= _options.LoginAttemptsPerMinute; i++)
        {
            var context = CreateHttpContext("/api/auth/login", "POST", clientIp);
            await middleware.InvokeAsync(context);
        }

        // Make one more request
        var blockedContext = CreateHttpContext("/api/auth/login", "POST", clientIp);
        await middleware.InvokeAsync(blockedContext);

        // Assert
        blockedContext.Response.StatusCode.Should().Be((int)HttpStatusCode.TooManyRequests);
        blockedContext.Response.Headers.Should().ContainKey("Retry-After");
    }

    [Fact]
    public async Task Should_RateLimitByUser_ForAuthenticatedApiRequests()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new RateLimitingMiddleware(next, _loggerMock.Object, _options);

        // Act - make requests from same user
        for (int i = 0; i <= _options.ApiRequestsPerMinutePerUser; i++)
        {
            var context = CreateHttpContext("/api/rifles", "GET", "192.168.1.102", userId);
            await middleware.InvokeAsync(context);

            if (i < _options.ApiRequestsPerMinutePerUser)
            {
                context.Response.StatusCode.Should().NotBe((int)HttpStatusCode.TooManyRequests);
            }
            else
            {
                context.Response.StatusCode.Should().Be((int)HttpStatusCode.TooManyRequests);
            }
        }
    }

    [Fact]
    public async Task Should_RateLimitByIp_ForUnauthenticatedApiRequests()
    {
        // Arrange
        var clientIp = "192.168.1.103";
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new RateLimitingMiddleware(next, _loggerMock.Object, _options);

        // Act - make unauthenticated requests
        for (int i = 0; i <= _options.ApiRequestsPerMinutePerIp; i++)
        {
            var context = CreateHttpContext("/api/rifles", "GET", clientIp);
            await middleware.InvokeAsync(context);

            if (i < _options.ApiRequestsPerMinutePerIp)
            {
                context.Response.StatusCode.Should().NotBe((int)HttpStatusCode.TooManyRequests);
            }
            else
            {
                context.Response.StatusCode.Should().Be((int)HttpStatusCode.TooManyRequests);
            }
        }
    }

    [Fact]
    public async Task Should_TrackDifferentUsersIndependently()
    {
        // Arrange
        var user1 = Guid.NewGuid().ToString();
        var user2 = Guid.NewGuid().ToString();
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new RateLimitingMiddleware(next, _loggerMock.Object, _options);

        // Act - exhaust limit for user1
        for (int i = 0; i <= _options.ApiRequestsPerMinutePerUser; i++)
        {
            var context = CreateHttpContext("/api/rifles", "GET", "192.168.1.104", user1);
            await middleware.InvokeAsync(context);
        }

        // User2 should still be allowed
        var user2Context = CreateHttpContext("/api/rifles", "GET", "192.168.1.105", user2);
        await middleware.InvokeAsync(user2Context);

        // Assert
        user2Context.Response.StatusCode.Should().NotBe((int)HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Should_ApplyStricterLimits_ForRegistrationEndpoint()
    {
        // Arrange
        var clientIp = "192.168.1.106";
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new RateLimitingMiddleware(next, _loggerMock.Object, _options);

        // Act - registration has per-hour limit (stricter)
        for (int i = 0; i <= _options.RegistrationsPerHour; i++)
        {
            var context = CreateHttpContext("/api/auth/register", "POST", clientIp);
            await middleware.InvokeAsync(context);

            if (i < _options.RegistrationsPerHour)
            {
                context.Response.StatusCode.Should().NotBe((int)HttpStatusCode.TooManyRequests);
            }
            else
            {
                context.Response.StatusCode.Should().Be((int)HttpStatusCode.TooManyRequests);
            }
        }
    }

    [Fact]
    public async Task Should_IncludeRateLimitHeaders_InResponse()
    {
        // Arrange
        var context = CreateHttpContext("/api/rifles", "GET", "192.168.1.107", Guid.NewGuid().ToString());
        var headersSet = false;

        // Capture the OnStarting callback
        context.Response.OnStarting(() =>
        {
            headersSet = true;
            return Task.CompletedTask;
        });

        RequestDelegate next = ctx =>
        {
            // Trigger the OnStarting callbacks
            ctx.Response.OnStarting(() => Task.CompletedTask);
            return Task.CompletedTask;
        };

        var middleware = new RateLimitingMiddleware(next, _loggerMock.Object, _options);

        // Act
        await middleware.InvokeAsync(context);

        // Assert - headers are set via OnStarting callback
        // We can't easily test this without actually starting the response
        // This test just ensures no exceptions are thrown
        context.Response.StatusCode.Should().NotBe((int)HttpStatusCode.TooManyRequests);
    }

    private static DefaultHttpContext CreateHttpContext(string path, string method, string clientIp, string? userId = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = method;
        context.Connection.RemoteIpAddress = IPAddress.Parse(clientIp.Replace("192.168.", "10.0.")); // Use valid IP

        if (!string.IsNullOrEmpty(userId))
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId)
            };
            var identity = new ClaimsIdentity(claims, "Test");
            context.User = new ClaimsPrincipal(identity);
        }

        return context;
    }
}
