using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Utilities;
using Maroik.Core.Service.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Maroik.Core.Service.Tests.Services;

public class ProfileServiceTests
{
    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly Mock<IPasswordService> _passwordService = new();
    private readonly Mock<IClamavClient> _clamavClient = new();
    private readonly IOptions<ServerSetting> _settings =
        Options.Create(new ServerSetting { ClamavHost = "localhost", ClamavPort = 3310 });

    private ProfileService CreateSut() => new(
        _accountRepo.Object,
        _passwordService.Object,
        _clamavClient.Object,
        _settings,
        NullLogger<ProfileService>.Instance);

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static AccountResponse ActiveAccount(string email = "user@example.com") => new()
    {
        Email = email,
        HashedPassword = "$2a$13$placeholder",
        Nickname = "TestUser",
        AvatarImagePath = "/upload/Management/Profile/default-avatar.jpg"
    };

    // ── GetProfileAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetProfileAsync_DelegatesToRepository()
    {
        var account = ActiveAccount();
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(account.Email!)).ReturnsAsync(account);
        var sut = CreateSut();

        AccountResponse? result = await sut.GetProfileAsync(account.Email!);

        Assert.NotNull(result);
        Assert.Equal(account.Email, result.Email);
    }

    // ── UpdateAvatarAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAvatarAsync_ReturnsFail_WhenAccountNotFound()
    {
        _accountRepo.Setup(r => r.GetAccountByEmailAsync("ghost@example.com")).ReturnsAsync((AccountResponse?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateAvatarAsync("ghost@example.com", "/new/path.jpg");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task UpdateAvatarAsync_UpdatesAvatarPath_WhenAccountFound()
    {
        var account = ActiveAccount();
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(account.Email!)).ReturnsAsync(account);
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<AccountRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateAvatarAsync(account.Email!, "/new/avatar.jpg");

        Assert.True(result.Success);
        _accountRepo.Verify(r => r.UpdateAccountAsync(
            It.Is<AccountRequest>(req => req.AvatarImagePath == "/new/avatar.jpg")), Times.Once);
    }

    // ── UpdateTimeZoneAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task UpdateTimeZoneAsync_ReturnsFail_WhenAccountNotFound()
    {
        _accountRepo.Setup(r => r.GetAccountByEmailAsync("ghost@example.com")).ReturnsAsync((AccountResponse?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateTimeZoneAsync("ghost@example.com", "Asia/Seoul");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task UpdateTimeZoneAsync_UpdatesTimeZone_WhenAccountFound()
    {
        var account = ActiveAccount();
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(account.Email!)).ReturnsAsync(account);
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<AccountRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateTimeZoneAsync(account.Email!, "Asia/Seoul");

        Assert.True(result.Success);
        _accountRepo.Verify(r => r.UpdateAccountAsync(
            It.Is<AccountRequest>(req => req.TimeZoneIanaId == "Asia/Seoul")), Times.Once);
    }

    // ── UpdatePasswordAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePasswordAsync_ReturnsFail_WhenAccountNotFound()
    {
        _accountRepo.Setup(r => r.GetAccountByEmailAsync("ghost@example.com")).ReturnsAsync((AccountResponse?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdatePasswordAsync("ghost@example.com", "old", "new");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task UpdatePasswordAsync_ReturnsFail_WhenCurrentPasswordIsWrong()
    {
        var account = ActiveAccount();
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(account.Email!)).ReturnsAsync(account);
        _passwordService.Setup(p => p.VerifyPassword("wrongpass", account.HashedPassword!)).Returns(false);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdatePasswordAsync(account.Email!, "wrongpass", "newpass");

        Assert.False(result.Success);
        Assert.Contains("Invalid password", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdatePasswordAsync_HashesAndUpdates_WhenCurrentPasswordIsCorrect()
    {
        var account = ActiveAccount();
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(account.Email!)).ReturnsAsync(account);
        _passwordService.Setup(p => p.VerifyPassword("oldpass", account.HashedPassword!)).Returns(true);
        _passwordService.Setup(p => p.HashPassword("newpass")).Returns("$2a$13$newhash");
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<AccountRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdatePasswordAsync(account.Email!, "oldpass", "newpass");

        Assert.True(result.Success);
        _accountRepo.Verify(r => r.UpdateAccountAsync(
            It.Is<AccountRequest>(req => req.HashedPassword == "$2a$13$newhash")), Times.Once);
    }
}
