using Maroik.Core.Contract.Dtos;
using Maroik.Core.Service.Services;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Maroik.Core.Service.Tests.Services;

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

    private SessionService CreateSut() => new(_httpContextAccessor.Object);

    private void SetupContext()
    {
        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(c => c.Session).Returns(_session.Object);
        _httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext.Object);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static AccountResponse SampleAccount() => new()
    {
        Email = "user@example.com",
        Nickname = "TestUser",
        Role = "User"
    };

    // ── GetAccount ───────────────────────────────────────────────────────────

    [Fact]
    public void GetAccount_ReturnsNull_WhenHttpContextIsNull()
    {
        _httpContextAccessor.Setup(a => a.HttpContext).Returns(((HttpContext?)null)!);
        var sut = CreateSut();

        AccountResponse? result = sut.GetAccount();

        Assert.Null(result);
    }

    [Fact]
    public void GetAccount_ReturnsNull_WhenSessionKeyDoesNotExist()
    {
        SetupContext();
        byte[] outBytes = null!;
        _session.Setup(s => s.TryGetValue(It.IsAny<string>(), out outBytes)).Returns(false);
        var sut = CreateSut();

        AccountResponse? result = sut.GetAccount();

        Assert.Null(result);
    }

    [Fact]
    public void GetAccount_ReturnsAccount_WhenValidSessionDataExists()
    {
        SetupContext();
        var account = SampleAccount();
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(
            System.Text.Json.JsonSerializer.Serialize(account));
        _session.Setup(s => s.TryGetValue(It.IsAny<string>(), out bytes)).Returns(true);
        var sut = CreateSut();

        AccountResponse? result = sut.GetAccount();

        Assert.NotNull(result);
        Assert.Equal(account.Email, result.Email);
        Assert.Equal(account.Nickname, result.Nickname);
    }

    [Fact]
    public void GetAccount_ReturnsNull_WhenSessionDataIsInvalidJson()
    {
        SetupContext();
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes("not valid json {{{{");
        _session.Setup(s => s.TryGetValue(It.IsAny<string>(), out bytes)).Returns(true);
        var sut = CreateSut();

        AccountResponse? result = sut.GetAccount();

        Assert.Null(result);
    }

    [Fact]
    public void GetAccount_ReturnsNull_WhenSessionBytesAreEmpty()
    {
        SetupContext();
        byte[] bytes = [];
        _session.Setup(s => s.TryGetValue(It.IsAny<string>(), out bytes)).Returns(true);
        var sut = CreateSut();

        AccountResponse? result = sut.GetAccount();

        Assert.Null(result);
    }

    // ── SetAccount ───────────────────────────────────────────────────────────

    [Fact]
    public void SetAccount_CallsSessionSet_WithSerializedAccount()
    {
        SetupContext();
        _session.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()));
        var sut = CreateSut();

        sut.SetAccount(SampleAccount());

        _session.Verify(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Once);
    }

    [Fact]
    public void SetAccount_DoesNotThrow_WhenHttpContextIsNull()
    {
        _httpContextAccessor.Setup(a => a.HttpContext).Returns(((HttpContext?)null)!);
        var sut = CreateSut();

        // Should not throw
        var ex = Record.Exception(() => sut.SetAccount(SampleAccount()));
        Assert.Null(ex);
    }

    // ── RemoveAccount ────────────────────────────────────────────────────────

    [Fact]
    public void RemoveAccount_CallsSessionRemove()
    {
        SetupContext();
        _session.Setup(s => s.Remove(It.IsAny<string>()));
        var sut = CreateSut();

        sut.RemoveAccount();

        _session.Verify(s => s.Remove(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void RemoveAccount_DoesNotThrow_WhenHttpContextIsNull()
    {
        _httpContextAccessor.Setup(a => a.HttpContext).Returns(((HttpContext?)null)!);
        var sut = CreateSut();

        var ex = Record.Exception(() => sut.RemoveAccount());
        Assert.Null(ex);
    }
}
