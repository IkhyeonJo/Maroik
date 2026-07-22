using Maroik.Website.Attributes;
using Maroik.Website.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Maroik.Website.Tests.Middlewares;

/// <summary>
/// Unit tests for <see cref="RegenerateSessionOnEntryMiddleware"/>, which strips the session
/// cookie from the incoming request when the resolved endpoint carries
/// <see cref="RegenerateSessionOnEntryAttribute"/>, forcing the downstream session middleware
/// to establish a brand-new session.
/// </summary>
public class RegenerateSessionOnEntryMiddlewareTests
{
    private const string SessionCookieName = ".Maroik.Session";

    private static IOptions<SessionOptions> SessionOptions() =>
        Options.Create(new SessionOptions { Cookie = { Name = SessionCookieName } });

    private static DefaultHttpContext BuildContext(bool withAttribute, string? cookieHeader)
    {
        var context = new DefaultHttpContext();
        if (cookieHeader != null)
        {
            context.Request.Headers.Cookie = cookieHeader;
        }

        var metadata = withAttribute
            ? new EndpointMetadataCollection(new RegenerateSessionOnEntryAttribute())
            : EndpointMetadataCollection.Empty;
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, metadata, "test"));

        return context;
    }

    [Fact]
    public async Task InvokeAsync_AttributePresent_SessionCookieOnly_RemovesCookieHeader()
    {
        var context = BuildContext(withAttribute: true, cookieHeader: $"{SessionCookieName}=abc123");
        bool nextCalled = false;
        var sut = new RegenerateSessionOnEntryMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await sut.InvokeAsync(context, SessionOptions());

        Assert.True(nextCalled);
        Assert.False(context.Request.Headers.ContainsKey("Cookie"));
    }

    [Fact]
    public async Task InvokeAsync_AttributePresent_SessionCookieAmongOthers_StripsOnlySessionCookie()
    {
        var context = BuildContext(withAttribute: true, cookieHeader: $"other=xyz; {SessionCookieName}=abc123; lang=ko-KR");
        bool nextCalled = false;
        var sut = new RegenerateSessionOnEntryMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await sut.InvokeAsync(context, SessionOptions());

        Assert.True(nextCalled);
        Assert.False(context.Request.Cookies.ContainsKey(SessionCookieName));
        Assert.Equal("xyz", context.Request.Cookies["other"]);
        Assert.Equal("ko-KR", context.Request.Cookies["lang"]);
    }

    [Fact]
    public async Task InvokeAsync_AttributePresent_NoCookieHeader_Forwards()
    {
        var context = BuildContext(withAttribute: true, cookieHeader: null);
        bool nextCalled = false;
        var sut = new RegenerateSessionOnEntryMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await sut.InvokeAsync(context, SessionOptions());

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_AttributePresent_NoSessionCookie_LeavesOtherCookiesIntact()
    {
        var context = BuildContext(withAttribute: true, cookieHeader: "other=xyz");
        bool nextCalled = false;
        var sut = new RegenerateSessionOnEntryMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await sut.InvokeAsync(context, SessionOptions());

        Assert.True(nextCalled);
        Assert.Equal("xyz", context.Request.Cookies["other"]);
    }

    [Fact]
    public async Task InvokeAsync_AttributeAbsent_LeavesSessionCookieIntact()
    {
        var context = BuildContext(withAttribute: false, cookieHeader: $"{SessionCookieName}=abc123");
        bool nextCalled = false;
        var sut = new RegenerateSessionOnEntryMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await sut.InvokeAsync(context, SessionOptions());

        Assert.True(nextCalled);
        Assert.Equal("abc123", context.Request.Cookies[SessionCookieName]);
    }

    [Fact]
    public async Task InvokeAsync_NoEndpoint_LeavesSessionCookieIntact()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = $"{SessionCookieName}=abc123";
        bool nextCalled = false;
        var sut = new RegenerateSessionOnEntryMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await sut.InvokeAsync(context, SessionOptions());

        Assert.True(nextCalled);
        Assert.Equal("abc123", context.Request.Cookies[SessionCookieName]);
    }
}
