using Maroik.Core.Contract.Domains.Account;
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
    private readonly Mock<IDomainEventDispatcher> _dispatcher = new();

    private ManagementAccountService CreateSut() => new(
        _accountRepo.Object,
        _passwordService.Object,
        _dispatcher.Object,
        NullLogger<ManagementAccountService>.Instance);

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Account ActiveAccount(
        string email = "user@example.com",
        bool locked = false,
        long loginAttempt = 0) =>
        Account.Reconstitute(
            email: email,
            hashedPassword: "$2a$13$placeholder",
            nickname: "User",
            avatarImagePath: null,
            role: "User",
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

    // ── GetAllAccountsAsync ──────────────────────────────────────────────────

    /// <summary>Verifies that <c>GetAllAccountsAsync</c> delegates to repository.</summary>
    [Fact]
    public async Task GetAllAccountsAsync_DelegatesToRepository()
    {
        _accountRepo.Setup(r => r.GetAllAsync()).ReturnsAsync([ActiveAccount()]);
        var sut = CreateSut();

        List<AccountResponse> result = await sut.GetAllAccountsAsync();

        Assert.Single(result);
    }

    // ── GetAccountByEmailAsync ───────────────────────────────────────────────

    /// <summary>Verifies that <c>GetAccountByEmailAsync</c> returns null when not found.</summary>
    [Fact]
    public async Task GetAccountByEmailAsync_ReturnsNull_WhenNotFound()
    {
        _accountRepo.Setup(r => r.GetAccountByEmailAsync("ghost@example.com")).ReturnsAsync((Account?)null);
        var sut = CreateSut();

        AccountResponse? result = await sut.GetAccountByEmailAsync("ghost@example.com");

        Assert.Null(result);
    }

    // ── CreateAccountAsync ───────────────────────────────────────────────────

    /// <summary>Verifies that <c>CreateAccountAsync</c> returns fail when email already exists.</summary>
    [Fact]
    public async Task CreateAccountAsync_ReturnsFail_WhenEmailAlreadyExists()
    {
        _accountRepo.Setup(r => r.GetAccountByEmailAsync("existing@example.com")).ReturnsAsync(ActiveAccount("existing@example.com"));
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAccountAsync(new AccountRequest
        {
            Email = "existing@example.com",
            HashedPassword = "plain"
        });

        Assert.False(result.Success);
        Assert.Contains("already been created", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>CreateAccountAsync</c> hashes password and creates account when email is new.</summary>
    [Fact]
    public async Task CreateAccountAsync_HashesPasswordAndCreatesAccount_WhenEmailIsNew()
    {
        _accountRepo.Setup(r => r.GetAccountByEmailAsync("new@example.com")).ReturnsAsync((Account?)null);
        _passwordService.Setup(p => p.HashPassword("plain")).Returns("$2a$13$hashed");
        _accountRepo.Setup(r => r.CreateAccountAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAccountAsync(new AccountRequest
        {
            Email = "new@example.com",
            HashedPassword = "plain",
            Nickname = "NewUser",
            EmailConfirmed = true
        });

        Assert.True(result.Success);
        _accountRepo.Verify(r => r.CreateAccountAsync(It.Is<Account>(a => a.HashedPassword == "$2a$13$hashed")), Times.Once);
    }

    // ── UpdateAccountAsync ───────────────────────────────────────────────────

    /// <summary>Verifies that <c>UpdateAccountAsync</c> returns fail when account not found.</summary>
    [Fact]
    public async Task UpdateAccountAsync_ReturnsFail_WhenAccountNotFound()
    {
        _accountRepo.Setup(r => r.GetAccountByEmailAsync("ghost@example.com")).ReturnsAsync((Account?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateAccountAsync(
            new AccountRequest { Email = "ghost@example.com" }, null);

        Assert.False(result.Success);
        Assert.Contains("wrong", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>UpdateAccountAsync</c> keeps existing password when new password is empty.</summary>
    [Fact]
    public async Task UpdateAccountAsync_KeepsExistingPassword_WhenNewPasswordIsEmpty()
    {
        var account = ActiveAccount();
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(account.Email.Value)).ReturnsAsync(account);
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateAccountAsync(
            new AccountRequest { Email = account.Email.Value, Role = "User" }, null);

        Assert.True(result.Success);
        _accountRepo.Verify(r => r.UpdateAccountAsync(
            It.Is<Account>(a => a.HashedPassword == account.HashedPassword)), Times.Once);
        _passwordService.Verify(p => p.HashPassword(It.IsAny<string>()), Times.Never);
    }

    /// <summary>Verifies that <c>UpdateAccountAsync</c> hashes new password when new password provided.</summary>
    [Fact]
    public async Task UpdateAccountAsync_HashesNewPassword_WhenNewPasswordProvided()
    {
        var account = ActiveAccount();
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(account.Email.Value)).ReturnsAsync(account);
        _passwordService.Setup(p => p.HashPassword("newpass")).Returns("$2a$13$newhash");
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateAccountAsync(
            new AccountRequest { Email = account.Email.Value, Role = "User" }, "newpass");

        Assert.True(result.Success);
        _accountRepo.Verify(r => r.UpdateAccountAsync(
            It.Is<Account>(a => a.HashedPassword == "$2a$13$newhash")), Times.Once);
    }

    /// <summary>Verifies that <c>UpdateAccountAsync</c> resets login attempt when unlocking.</summary>
    [Fact]
    public async Task UpdateAccountAsync_ResetsLoginAttempt_WhenUnlocking()
    {
        var account = ActiveAccount(locked: true, loginAttempt: 5);
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(account.Email.Value)).ReturnsAsync(account);
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateAccountAsync(
            new AccountRequest { Email = account.Email.Value, Locked = false }, null);

        Assert.True(result.Success);
        _accountRepo.Verify(r => r.UpdateAccountAsync(
            It.Is<Account>(a => a.Locked == false && a.LoginAttempt == 0)), Times.Once);
    }

    // ── DeleteAccountAsync ───────────────────────────────────────────────────

    /// <summary>Verifies that <c>DeleteAccountAsync</c> returns fail when account not found.</summary>
    [Fact]
    public async Task DeleteAccountAsync_ReturnsFail_WhenAccountNotFound()
    {
        _accountRepo.Setup(r => r.GetAccountByEmailAsync("ghost@example.com")).ReturnsAsync((Account?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAccountAsync("ghost@example.com");

        Assert.False(result.Success);
        Assert.Contains("find", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>DeleteAccountAsync</c> sets deleted flag when account found.</summary>
    [Fact]
    public async Task DeleteAccountAsync_SetsDeletedFlag_WhenAccountFound()
    {
        var account = ActiveAccount();
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(account.Email.Value)).ReturnsAsync(account);
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAccountAsync(account.Email.Value);

        Assert.True(result.Success);
        _accountRepo.Verify(r => r.UpdateAccountAsync(
            It.Is<Account>(a => a.Deleted == true)), Times.Once);
    }
}
