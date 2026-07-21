using Maroik.Core.Contract.Constants;
using Maroik.Core.Contract.Dtos;
using Maroik.Website.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Session;
using Microsoft.Extensions.Options;
using Moq;

namespace Maroik.Website.Tests.Services;

/// <summary>
/// Unit tests for <see cref="SessionService"/>.
/// <see cref="IHttpContextAccessor"/> and <see cref="ISession"/> are replaced with Moq mocks.
/// Verifies that <c>SetAccount</c> serialises an <see cref="Maroik.Core.Contract.Dtos.AccountResponse"/>
/// to JSON and that <c>GetAccount</c> correctly deserialises it back.
/// </summary>
public class SessionServiceTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
    private readonly Mock<ISession> _session = new();
    private readonly Mock<ISessionStore> _sessionStore = new();
    private readonly IOptions<SessionOptions> _sessionOptions = Options.Create(new SessionOptions());

    private SessionService CreateSut() => new(_httpContextAccessor.Object, _sessionStore.Object, _sessionOptions);

    private void SetupContext()
    {
        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(c => c.Session).Returns(_session.Object);
        _httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext.Object);
    }

    // -- Helpers --------------------------------------------------------------

    private static AccountResponse SampleAccount() => new()
    {
        Email = "user@example.com",
        Nickname = "TestUser",
        Role = Role.User
    };

    // -- GetAccount -----------------------------------------------------------

    /// <summary>Verifies that <c>GetAccount</c> returns null when http context is null.</summary>
    [Fact]
    public void GetAccount_ReturnsNull_WhenHttpContextIsNull()
    {
        _httpContextAccessor.Setup(a => a.HttpContext).Returns(((HttpContext?)null)!);
        var sut = CreateSut();

        AccountResponse? result = sut.GetAccount();

        Assert.Null(result);
    }

    /// <summary>Verifies that <c>GetAccount</c> returns null when session key does not exist.</summary>
    [Fact]
    public void GetAccount_ReturnsNull_WhenSessionKeyDoesNotExist()
    {
        SetupContext();
        byte[]? outBytes = null!;
        _session.Setup(s => s.TryGetValue(It.IsAny<string>(), out outBytes)).Returns(false);
        var sut = CreateSut();

        AccountResponse? result = sut.GetAccount();

        Assert.Null(result);
    }

    /// <summary>Verifies that <c>GetAccount</c> returns account when valid session data exists.</summary>
    [Fact]
    public void GetAccount_ReturnsAccount_WhenValidSessionDataExists()
    {
        SetupContext();
        var account = SampleAccount();
        byte[]? bytes = System.Text.Encoding.UTF8.GetBytes(
            System.Text.Json.JsonSerializer.Serialize(account));
        _session.Setup(s => s.TryGetValue(It.IsAny<string>(), out bytes)).Returns(true);
        var sut = CreateSut();

        AccountResponse? result = sut.GetAccount();

        Assert.NotNull(result);
        Assert.Equal(account.Email, result.Email);
        Assert.Equal(account.Nickname, result.Nickname);
    }

    /// <summary>Verifies that <c>GetAccount</c> returns null when session data is invalid json.</summary>
    [Fact]
    public void GetAccount_ReturnsNull_WhenSessionDataIsInvalidJson()
    {
        SetupContext();
        byte[]? bytes = System.Text.Encoding.UTF8.GetBytes("not valid json {{{{");
        _session.Setup(s => s.TryGetValue(It.IsAny<string>(), out bytes)).Returns(true);
        var sut = CreateSut();

        AccountResponse? result = sut.GetAccount();

        Assert.Null(result);
    }

    /// <summary>Verifies that <c>GetAccount</c> returns null when session bytes are empty.</summary>
    [Fact]
    public void GetAccount_ReturnsNull_WhenSessionBytesAreEmpty()
    {
        SetupContext();
        byte[]? bytes = [];
        _session.Setup(s => s.TryGetValue(It.IsAny<string>(), out bytes)).Returns(true);
        var sut = CreateSut();

        AccountResponse? result = sut.GetAccount();

        Assert.Null(result);
    }

    // -- SetAccount -----------------------------------------------------------

    /// <summary>Verifies that <c>SetAccount</c> calls session set with serialized account.</summary>
    [Fact]
    public void SetAccount_CallsSessionSet_WithSerializedAccount()
    {
        SetupContext();
        _session.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()));
        var sut = CreateSut();

        sut.SetAccount(SampleAccount());

        _session.Verify(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Once);
    }

    /// <summary>Verifies that <c>SetAccount</c> does not throw when http context is null.</summary>
    [Fact]
    public void SetAccount_DoesNotThrow_WhenHttpContextIsNull()
    {
        _httpContextAccessor.Setup(a => a.HttpContext).Returns(((HttpContext?)null)!);
        var sut = CreateSut();

        var ex = Record.Exception(() => sut.SetAccount(SampleAccount()));
        Assert.Null(ex);
    }

    // -- RemoveAccount --------------------------------------------------------

    /// <summary>Verifies that <c>RemoveAccount</c> calls session remove.</summary>
    [Fact]
    public void RemoveAccount_CallsSessionRemove()
    {
        SetupContext();
        _session.Setup(s => s.Remove(It.IsAny<string>()));
        var sut = CreateSut();

        sut.RemoveAccount();

        _session.Verify(s => s.Remove(It.IsAny<string>()), Times.Once);
    }

    /// <summary>Verifies that <c>RemoveAccount</c> does not throw when http context is null.</summary>
    [Fact]
    public void RemoveAccount_DoesNotThrow_WhenHttpContextIsNull()
    {
        _httpContextAccessor.Setup(a => a.HttpContext).Returns(((HttpContext?)null)!);
        var sut = CreateSut();

        var ex = Record.Exception(() => sut.RemoveAccount());
        Assert.Null(ex);
    }

    // -- RegenerateSessionId ----------------------------------------------------

    /// <summary>
    /// Verifies that <c>RegenerateSessionId</c> replaces the request's session with a freshly
    /// created one (via <see cref="ISessionStore"/>) rather than reusing whatever session the
    /// request arrived with — this is the session-fixation defense itself, so it is asserted
    /// directly rather than through an implementation detail.
    /// </summary>
    [Fact]
    public void RegenerateSessionId_ReplacesSessionFeature_WithNewlyCreatedSession()
    {
        // DefaultHttpContext is used here (instead of the Mock<HttpContext> the other tests use)
        // because it has a real, working Features collection, which Set<T>/Get<T> need.
        var httpContext = new DefaultHttpContext();
        var originalSession = new Mock<ISession>().Object;
        httpContext.Features.Set<ISessionFeature>(new SessionFeature { Session = originalSession });
        _httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        var freshSession = new Mock<ISession>().Object;
        _sessionStore
            .Setup(s => s.Create(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<Func<bool>>(), true))
            .Returns(freshSession);
        var sut = CreateSut();

        sut.RegenerateSessionId();

        ISession? resultingSession = httpContext.Features.Get<ISessionFeature>()?.Session;
        Assert.Same(freshSession, resultingSession);
        Assert.NotSame(originalSession, resultingSession);
    }

    /// <summary>Verifies that <c>RegenerateSessionId</c> requests a new session key each time, not a reused/fixed one.</summary>
    [Fact]
    public void RegenerateSessionId_RequestsANewSessionKey()
    {
        var httpContext = new DefaultHttpContext();
        _httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);
        string? capturedKey = null;
        _sessionStore
            .Setup(s => s.Create(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<Func<bool>>(), true))
            .Callback<string, TimeSpan, TimeSpan, Func<bool>, bool>((key, _, _, _, _) => capturedKey = key)
            .Returns(new Mock<ISession>().Object);
        var sut = CreateSut();

        sut.RegenerateSessionId();

        Assert.False(string.IsNullOrEmpty(capturedKey));
    }

    /// <summary>Verifies that <c>RegenerateSessionId</c> does not throw when http context is null.</summary>
    [Fact]
    public void RegenerateSessionId_DoesNotThrow_WhenHttpContextIsNull()
    {
        _httpContextAccessor.Setup(a => a.HttpContext).Returns(((HttpContext?)null)!);
        var sut = CreateSut();

        var ex = Record.Exception(() => sut.RegenerateSessionId());
        Assert.Null(ex);
    }
}
