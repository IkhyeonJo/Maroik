using Maroik.Core.Contract.Misc.Constants;
using Maroik.Core.Domain.Account;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Service.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ManagementAccountService"/>.
/// All repository and password-service dependencies are replaced with Moq mocks.
/// Covers admin-side account management: list retrieval, create, update,
/// password reset, lock/unlock, and delete operations.
/// </summary>
public class ManagementAccountServiceTests
{
    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly Mock<IPasswordService> _passwordService = new();

    private ManagementAccountService CreateSut() => new(
        _accountRepo.Object,
        _passwordService.Object,
        NullLogger<ManagementAccountService>.Instance);

    // -- Helpers --------------------------------------------------------------

    private static Account ActiveAccount(
        string email = "user@example.com",
        bool locked = false,
        long loginAttempt = 0) =>
        Account.Reconstitute(
            email: email,
            hashedPassword: "$2a$13$placeholder",
            nickname: "User",
            avatarImagePath: null,
            role: Role.User,
            timeZoneIanaId: "UTC",
            defaultMonetaryUnit: null,
            locked: locked,
            loginAttempt: loginAttempt,
            emailConfirmed: true,
            agreedServiceTerms: true,
            registrationToken: null,
            resetPasswordToken: null,
            created: DateTime.UtcNow,
            updated: DateTime.UtcNow,
            message: null,
            deleted: false);

    // -- GetAllAccountsAsync --------------------------------------------------

    /// <summary>Verifies that <c>GetAllAccountsAsync</c> delegates to repository.</summary>
    [Fact]
    public async Task GetAllAccountsAsync_DelegatesToRepository()
    {
        _accountRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([ActiveAccount()]);
        var sut = CreateSut();

        List<AccountResponse> result = await sut.GetAllAccountsAsync(TestContext.Current.CancellationToken);

        Assert.Single(result);
    }

    // -- GetAccountByEmailAsync -----------------------------------------------

    /// <summary>Verifies that <c>GetAccountByEmailAsync</c> returns null when not found.</summary>
    [Fact]
    public async Task GetAccountByEmailAsync_ReturnsNull_WhenNotFound()
    {
        _accountRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Account?)null);
        var sut = CreateSut();

        AccountResponse? result = await sut.GetAccountByEmailAsync("ghost@example.com", TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    // -- CreateAccountAsync ---------------------------------------------------

    /// <summary>Verifies that <c>CreateAccountAsync</c> returns fail when email already exists.</summary>
    [Fact]
    public async Task CreateAccountAsync_ReturnsFail_WhenEmailAlreadyExists()
    {
        _accountRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(ActiveAccount("existing@example.com"));
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAccountAsync(new AccountRequest
        {
            Email = "existing@example.com",
            HashedPassword = "plain"
        }, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("already been created", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>CreateAccountAsync</c> hashes password and creates account when email is new.</summary>
    [Fact]
    public async Task CreateAccountAsync_HashesPasswordAndCreatesAccount_WhenEmailIsNew()
    {
        _accountRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Account?)null);
        _passwordService.Setup(p => p.HashPassword("plain")).Returns("$2a$13$hashed");
        _accountRepo.Setup(r => r.CreateAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAccountAsync(new AccountRequest
        {
            Email = "new@example.com",
            HashedPassword = "plain",
            Nickname = "NewUser",
            EmailConfirmed = true
        }, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        _accountRepo.Verify(r => r.CreateAsync(It.Is<Account>(a => a.HashedPassword == "$2a$13$hashed"), It.IsAny<CancellationToken>()), Times.Once);
    }

    // -- UpdateAccountAsync ---------------------------------------------------

    /// <summary>Verifies that <c>UpdateAccountAsync</c> returns fail when account not found.</summary>
    [Fact]
    public async Task UpdateAccountAsync_ReturnsFail_WhenAccountNotFound()
    {
        _accountRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Account?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateAccountAsync(
            new AccountRequest { Email = "ghost@example.com" }, null, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("wrong", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>UpdateAccountAsync</c> keeps existing password when new password is empty.</summary>
    [Fact]
    public async Task UpdateAccountAsync_KeepsExistingPassword_WhenNewPasswordIsEmpty()
    {
        var account = ActiveAccount();
        _accountRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _accountRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateAccountAsync(
            new AccountRequest { Email = account.Email.Value, Role = "User" }, null, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        _accountRepo.Verify(r => r.UpdateEntityAsync(
            It.Is<Account>(a => a.HashedPassword == account.HashedPassword), It.IsAny<CancellationToken>()), Times.Once);
        _passwordService.Verify(p => p.HashPassword(It.IsAny<string>()), Times.Never);
    }

    /// <summary>Verifies that <c>UpdateAccountAsync</c> hashes new password when new password provided.</summary>
    [Fact]
    public async Task UpdateAccountAsync_HashesNewPassword_WhenNewPasswordProvided()
    {
        var account = ActiveAccount();
        _accountRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _passwordService.Setup(p => p.HashPassword("newpass")).Returns("$2a$13$newhash");
        _accountRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateAccountAsync(
            new AccountRequest { Email = account.Email.Value, Role = "User" }, "newpass", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        _accountRepo.Verify(r => r.UpdateEntityAsync(
            It.Is<Account>(a => a.HashedPassword == "$2a$13$newhash"), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Verifies that <c>UpdateAccountAsync</c> resets login attempt when unlocking.</summary>
    [Fact]
    public async Task UpdateAccountAsync_ResetsLoginAttempt_WhenUnlocking()
    {
        var account = ActiveAccount(locked: true, loginAttempt: 5);
        _accountRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _accountRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateAccountAsync(
            new AccountRequest { Email = account.Email.Value, Locked = false }, null, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        _accountRepo.Verify(r => r.UpdateEntityAsync(
            It.Is<Account>(a => a.Locked == false && a.LoginAttempt == 0), It.IsAny<CancellationToken>()), Times.Once);
    }

    // -- DeleteAccountAsync ---------------------------------------------------

    /// <summary>Verifies that <c>DeleteAccountAsync</c> returns fail when account not found.</summary>
    [Fact]
    public async Task DeleteAccountAsync_ReturnsFail_WhenAccountNotFound()
    {
        _accountRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Account?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAccountAsync("ghost@example.com", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("find", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>DeleteAccountAsync</c> sets deleted flag when account found.</summary>
    [Fact]
    public async Task DeleteAccountAsync_SetsDeletedFlag_WhenAccountFound()
    {
        var account = ActiveAccount();
        _accountRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _accountRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAccountAsync(account.Email.Value, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        _accountRepo.Verify(r => r.UpdateEntityAsync(
            It.Is<Account>(a => a.Deleted == true), It.IsAny<CancellationToken>()), Times.Once);
    }
}
