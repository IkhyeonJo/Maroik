using Maroik.Core.Contract.Constants;
using Maroik.Core.Contract.Dtos;
using Maroik.Website.Contracts;
using Maroik.Website.Middlewares;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Maroik.Website.Tests.Middlewares;

/// <summary>
/// Unit tests for <see cref="RoleBasedStaticFileMiddleware"/>, which blocks (403) requests
/// under <c>/admin/</c> for non-Admin sessions and under <c>/user/</c> for non-User sessions,
/// and forwards every other request unchanged.
/// </summary>
public class RoleBasedStaticFileMiddlewareTests
{
    private readonly Mock<ISessionService> _sessionService = new();

    private static DefaultHttpContext BuildContext(string path) =>
        new() { Request = { Path = path } };

    private void SetupAccount(string? role)
    {
        _sessionService
            .Setup(s => s.GetAccount())
            .Returns(role == null ? null : new AccountResponse { Email = "test@test.com", Nickname = "Test", Role = role });
    }

    [Fact]
    public async Task InvokeAsync_AdminPath_AdminSession_Forwards()
    {
        SetupAccount(Role.Admin);
        var context = BuildContext("/admin/dist/img/logo.png");
        bool nextCalled = false;
        var sut = new RoleBasedStaticFileMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await sut.InvokeAsync(context, _sessionService.Object);

        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_AdminPath_UserSession_Blocks()
    {
        SetupAccount(Role.User);
        var context = BuildContext("/admin/dist/img/logo.png");
        bool nextCalled = false;
        var sut = new RoleBasedStaticFileMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await sut.InvokeAsync(context, _sessionService.Object);

        Assert.False(nextCalled);
        Assert.Equal(403, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_AdminPath_NoSession_Blocks()
    {
        SetupAccount(null);
        var context = BuildContext("/admin/dist/img/logo.png");
        bool nextCalled = false;
        var sut = new RoleBasedStaticFileMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await sut.InvokeAsync(context, _sessionService.Object);

        Assert.False(nextCalled);
        Assert.Equal(403, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_UserPath_UserSession_Forwards()
    {
        SetupAccount(Role.User);
        var context = BuildContext("/user/dist/img/avatar.png");
        bool nextCalled = false;
        var sut = new RoleBasedStaticFileMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await sut.InvokeAsync(context, _sessionService.Object);

        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_UserPath_AdminSession_Blocks()
    {
        SetupAccount(Role.Admin);
        var context = BuildContext("/user/dist/img/avatar.png");
        bool nextCalled = false;
        var sut = new RoleBasedStaticFileMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await sut.InvokeAsync(context, _sessionService.Object);

        Assert.False(nextCalled);
        Assert.Equal(403, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_UserPath_NoSession_Blocks()
    {
        SetupAccount(null);
        var context = BuildContext("/user/dist/img/avatar.png");
        bool nextCalled = false;
        var sut = new RoleBasedStaticFileMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await sut.InvokeAsync(context, _sessionService.Object);

        Assert.False(nextCalled);
        Assert.Equal(403, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_UnrestrictedPath_NoSession_Forwards()
    {
        SetupAccount(null);
        var context = BuildContext("/css/site.css");
        bool nextCalled = false;
        var sut = new RoleBasedStaticFileMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await sut.InvokeAsync(context, _sessionService.Object);

        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_EmptyPath_Forwards()
    {
        SetupAccount(null);
        var context = BuildContext("");
        bool nextCalled = false;
        var sut = new RoleBasedStaticFileMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await sut.InvokeAsync(context, _sessionService.Object);

        Assert.True(nextCalled);
    }
}
