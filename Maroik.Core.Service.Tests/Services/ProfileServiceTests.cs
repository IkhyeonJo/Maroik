using Maroik.Core.Contract.Domains.Account;
using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Utilities;
using Maroik.Core.Service.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ProfileService"/>.
/// All repository and dependency-service mocks are provided via Moq.
/// Covers profile retrieval, nickname update, avatar update,
/// and password-change validation (current-password verification before update).
/// </summary>
public class ProfileServiceTests
{
    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly Mock<IAssetRepository> _assetRepo = new();
    private readonly Mock<IPasswordService> _passwordService = new();
    private readonly Mock<IClamavClient> _clamavClient = new();
    private readonly IOptions<ServerSetting> _settings =
        Options.Create(new ServerSetting { ClamavHost = "localhost", ClamavPort = 3310 });

    private ProfileService CreateSut() => new(
        _accountRepo.Object,
        _assetRepo.Object,
        _passwordService.Object,
        _clamavClient.Object,
        _settings,
        NullLogger<ProfileService>.Instance);

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Asset MakeAsset(string name, string unit = "KRW") =>
        Asset.Reconstitute(
            productName: name,
            accountEmail: "user@example.com",
            item: "Deposit",
            amount: 1000m,
            monetaryUnit: unit,
            note: null,
            deleted: false,
            created: DateTime.UtcNow,
            updated: DateTime.UtcNow);

    private static Account ActiveAccount(string email = "user@example.com") =>
        Account.Reconstitute(
            email: email,
            hashedPassword: "$2a$13$placeholder",
            nickname: "TestUser",
            avatarImagePath: "/upload/Management/Profile/default-avatar.jpg",
            role: "User",
            timeZoneIanaId: "UTC",
            defaultMonetaryUnit: null,
            locked: false,
            loginAttempt: 0,
            emailConfirmed: true,
            agreedServiceTerms: true,
            registrationToken: null,
            resetPasswordToken: null,
            created: DateTime.UtcNow,
            updated: DateTime.UtcNow,
            message: null,
            deleted: false);

    // ── GetProfileAsync ──────────────────────────────────────────────────────

    /// <summary>Verifies that <c>GetProfileAsync</c> delegates to repository.</summary>
    [Fact]
    public async Task GetProfileAsync_DelegatesToRepository()
    {
        var account = ActiveAccount();
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(account.Email.Value)).ReturnsAsync(account);
        var sut = CreateSut();

        AccountResponse? result = await sut.GetProfileAsync(account.Email.Value);

        Assert.NotNull(result);
        Assert.Equal(account.Email.Value, result.Email);
    }

    // ── UpdateAvatarAsync ────────────────────────────────────────────────────

    /// <summary>Verifies that <c>UpdateAvatarAsync</c> returns fail when account not found.</summary>
    [Fact]
    public async Task UpdateAvatarAsync_ReturnsFail_WhenAccountNotFound()
    {
        _accountRepo.Setup(r => r.GetAccountByEmailAsync("ghost@example.com")).ReturnsAsync((Account?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateAvatarAsync("ghost@example.com", "/new/path.jpg");

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>UpdateAvatarAsync</c> updates avatar path when account found.</summary>
    [Fact]
    public async Task UpdateAvatarAsync_UpdatesAvatarPath_WhenAccountFound()
    {
        var account = ActiveAccount();
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(account.Email.Value)).ReturnsAsync(account);
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateAvatarAsync(account.Email.Value, "/new/avatar.jpg");

        Assert.True(result.Success);
        _accountRepo.Verify(r => r.UpdateAccountAsync(
            It.Is<Account>(a => a.AvatarImagePath == "/new/avatar.jpg")), Times.Once);
    }

    // ── UpdateTimeZoneAsync ──────────────────────────────────────────────────

    /// <summary>Verifies that <c>UpdateTimeZoneAsync</c> returns fail when account not found.</summary>
    [Fact]
    public async Task UpdateTimeZoneAsync_ReturnsFail_WhenAccountNotFound()
    {
        _accountRepo.Setup(r => r.GetAccountByEmailAsync("ghost@example.com")).ReturnsAsync((Account?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateTimeZoneAsync("ghost@example.com", "Asia/Seoul");

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>UpdateTimeZoneAsync</c> updates time zone when account found.</summary>
    [Fact]
    public async Task UpdateTimeZoneAsync_UpdatesTimeZone_WhenAccountFound()
    {
        var account = ActiveAccount();
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(account.Email.Value)).ReturnsAsync(account);
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateTimeZoneAsync(account.Email.Value, "Asia/Seoul");

        Assert.True(result.Success);
        _accountRepo.Verify(r => r.UpdateAccountAsync(
            It.Is<Account>(a => a.TimeZone.Value == "Asia/Seoul")), Times.Once);
    }

    // ── UpdatePasswordAsync ──────────────────────────────────────────────────

    /// <summary>Verifies that <c>UpdatePasswordAsync</c> returns fail when account not found.</summary>
    [Fact]
    public async Task UpdatePasswordAsync_ReturnsFail_WhenAccountNotFound()
    {
        _accountRepo.Setup(r => r.GetAccountByEmailAsync("ghost@example.com")).ReturnsAsync((Account?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdatePasswordAsync("ghost@example.com", "old", "new");

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>UpdatePasswordAsync</c> returns fail when current password is wrong.</summary>
    [Fact]
    public async Task UpdatePasswordAsync_ReturnsFail_WhenCurrentPasswordIsWrong()
    {
        var account = ActiveAccount();
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(account.Email.Value)).ReturnsAsync(account);
        _passwordService.Setup(p => p.VerifyPassword("wrongpass", account.HashedPassword)).Returns(false);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdatePasswordAsync(account.Email.Value, "wrongpass", "newpass");

        Assert.False(result.Success);
        Assert.Contains("Invalid password", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>UpdatePasswordAsync</c> hashes and updates when current password is correct.</summary>
    [Fact]
    public async Task UpdatePasswordAsync_HashesAndUpdates_WhenCurrentPasswordIsCorrect()
    {
        var account = ActiveAccount();
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(account.Email.Value)).ReturnsAsync(account);
        _passwordService.Setup(p => p.VerifyPassword("oldpass", account.HashedPassword)).Returns(true);
        _passwordService.Setup(p => p.HashPassword("newpass")).Returns("$2a$13$newhash");
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdatePasswordAsync(account.Email.Value, "oldpass", "newpass");

        Assert.True(result.Success);
        _accountRepo.Verify(r => r.UpdateAccountAsync(
            It.Is<Account>(a => a.HashedPassword == "$2a$13$newhash")), Times.Once);
    }

    // ── UpdateDefaultMonetaryUnitAsync ──────────────────────────────────────

    /// <summary>Verifies that <c>UpdateDefaultMonetaryUnitAsync</c> does nothing when account not found.</summary>
    [Fact]
    public async Task UpdateDefaultMonetaryUnitAsync_DoesNothing_WhenAccountNotFound()
    {
        _assetRepo.Setup(r => r.GetAssetsAsync("missing@example.com")).ReturnsAsync([]);
        _accountRepo.Setup(r => r.GetAccountByEmailAsync("missing@example.com")).ReturnsAsync((Account?)null);
        var sut = CreateSut();

        await sut.UpdateDefaultMonetaryUnitAsync("missing@example.com", "KRW");

        _accountRepo.Verify(r => r.UpdateAccountAsync(It.IsAny<Account>()), Times.Never);
    }

    /// <summary>Verifies that <c>UpdateDefaultMonetaryUnitAsync</c> persists unit when it exists in assets.</summary>
    [Fact]
    public async Task UpdateDefaultMonetaryUnitAsync_SetsUnit_WhenUnitExistsInAssets()
    {
        var account = ActiveAccount();
        _assetRepo.Setup(r => r.GetAssetsAsync(account.Email.Value)).ReturnsAsync([MakeAsset("Wallet", "KRW")]);
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(account.Email.Value)).ReturnsAsync(account);
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        await sut.UpdateDefaultMonetaryUnitAsync(account.Email.Value, "KRW");

        _accountRepo.Verify(r => r.UpdateAccountAsync(
            It.Is<Account>(a => a.DefaultMonetaryUnit == "KRW")), Times.Once);
    }

    /// <summary>Verifies that <c>UpdateDefaultMonetaryUnitAsync</c> clears unit when it is not in assets.</summary>
    [Fact]
    public async Task UpdateDefaultMonetaryUnitAsync_SetsNull_WhenUnitNotInAssets()
    {
        var account = ActiveAccount();
        _assetRepo.Setup(r => r.GetAssetsAsync(account.Email.Value)).ReturnsAsync([MakeAsset("Wallet", "USD")]);
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(account.Email.Value)).ReturnsAsync(account);
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        await sut.UpdateDefaultMonetaryUnitAsync(account.Email.Value, "KRW");

        _accountRepo.Verify(r => r.UpdateAccountAsync(
            It.Is<Account>(a => a.DefaultMonetaryUnit == null)), Times.Once);
    }
}
