using System.Security.Claims;
using eiupdds_backend.Middlewares;
using eiupdds_backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace eiupdds_backend.Tests.Middlewares;

public class ActiveUserMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenUserNotAuthenticated_CallsNext()
    {
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ActiveUserMiddleware(next);
        var guardMock = new Mock<IUserActiveGuard>();
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context, guardMock.Object);

        Assert.True(nextCalled);
        Assert.NotEqual(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenUserAuthenticatedAndActive_CallsNext()
    {
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ActiveUserMiddleware(next);
        var guardMock = new Mock<IUserActiveGuard>();
        guardMock.Setup(g => g.IsUserActiveAsync("user-1")).ReturnsAsync(true);

        var context = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-1")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        context.User = new ClaimsPrincipal(identity);

        await middleware.InvokeAsync(context, guardMock.Object);

        Assert.True(nextCalled);
        Assert.NotEqual(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenUserAuthenticatedAndInactive_ReturnsUnauthorized()
    {
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ActiveUserMiddleware(next);
        var guardMock = new Mock<IUserActiveGuard>();
        guardMock.Setup(g => g.IsUserActiveAsync("user-1")).ReturnsAsync(false);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-1")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        context.User = new ClaimsPrincipal(identity);

        await middleware.InvokeAsync(context, guardMock.Object);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }
}
