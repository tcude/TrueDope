using FluentAssertions;
using Microsoft.AspNetCore.Http;
using TrueDope.Api.Middleware;

namespace TrueDope.Api.Tests.Middleware;

public class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task Should_CallNextMiddleware()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = new SecurityHeadersMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Should_AddSecurityHeaders_WhenResponseStarts()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";

        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert - all headers should be present
        context.Response.Headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
        context.Response.Headers["X-Frame-Options"].ToString().Should().Be("DENY");
        context.Response.Headers["X-XSS-Protection"].ToString().Should().Be("1; mode=block");
        context.Response.Headers["Referrer-Policy"].ToString().Should().Be("strict-origin-when-cross-origin");
        context.Response.Headers["Content-Security-Policy"].ToString()
            .Should().Contain("default-src 'none'");
        context.Response.Headers["Permissions-Policy"].ToString()
            .Should().Contain("geolocation=()");
    }

    [Fact]
    public async Task Should_AddCacheControlHeader_ForApiEndpoints()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/rifles";

        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers["Cache-Control"].ToString()
            .Should().Contain("no-store");
        context.Response.Headers["Pragma"].ToString()
            .Should().Contain("no-cache");
    }

    [Fact]
    public async Task Should_NotAddCacheControlHeader_ForNonApiEndpoints()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/health";

        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert - Cache-Control should not be set for non-API paths
        context.Response.Headers.ContainsKey("Cache-Control").Should().BeFalse();
    }

    [Fact]
    public async Task Should_NotOverrideExistingHeaders()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";

        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert - should keep the existing value
        context.Response.Headers["X-Frame-Options"].ToString().Should().Be("SAMEORIGIN");
    }

    [Fact]
    public async Task Should_AddXContentTypeOptionsHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
    }

    [Fact]
    public async Task Should_AddXFrameOptionsHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers["X-Frame-Options"].ToString().Should().Be("DENY");
    }

    [Fact]
    public async Task Should_AddXXssProtectionHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers["X-XSS-Protection"].ToString().Should().Be("1; mode=block");
    }

    [Fact]
    public async Task Should_AddReferrerPolicyHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers["Referrer-Policy"].ToString().Should().Be("strict-origin-when-cross-origin");
    }

    [Fact]
    public async Task Should_AddContentSecurityPolicyHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers["Content-Security-Policy"].ToString()
            .Should().Contain("default-src 'none'");
    }

    [Fact]
    public async Task Should_AddPermissionsPolicyHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers["Permissions-Policy"].ToString()
            .Should().Contain("geolocation=()");
    }
}
