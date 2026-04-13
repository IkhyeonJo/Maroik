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

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static AccountResponse ActiveAccount(string email = "user@example.com") => new()
    {
        Email = email,
        HashedPassword = "$2a$13$placeholder",
        Nickname = "User",
        Role = "User",
        Locked = false,
        Deleted = false,
        EmailConfirmed = true,
        AgreedServiceTerms = true,
        LoginAttempt = 0
    };

    // ── GetAllAccountsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAccountsAsync_DelegatesToRepository()
    {
        _accountRepo.Setup(r => r.GetAllAsync()).ReturnsAsync([ActiveAccount()]);
        var sut = CreateSut();

        List<AccountResponse> result = await sut.GetAllAccountsAsync();

        Assert.Single(result);
    }

    // ── GetAccountByEmailAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetAccountByEmailAsync_ReturnsNull_WhenNotFound()
    {
        _accountRepo.Setup(r => r.GetAccountByEmailAsync("ghost@example.com")).ReturnsAsync((AccountResponse?)null);
        var sut = CreateSut();

        AccountResponse? result = await sut.GetAccountByEmailAsync("ghost@example.com");

        Assert.Null(result);
    }

    // ── CreateAccountAsync ───────────────────────────────────────────────────

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

    [Fact]
    public async Task CreateAccountAsync_HashesPasswordAndCreatesAccount_WhenEmailIsNew()
    {
        _accountRepo.Setup(r => r.GetAccountByEmailAsync("new@example.com")).ReturnsAsync((AccountResponse?)null);
        _passwordService.Setup(p => p.HashPassword("plain")).Returns("$2a$13$hashed");
        _accountRepo.Setup(r => r.CreateAccountAsync(It.IsAny<AccountRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAccountAsync(new AccountRequest
        {
            Email = "new@example.com",
            HashedPassword = "plain"
        });

        Assert.True(result.Success);
        _accountRepo.Verify(r => r.CreateAccountAsync(It.Is<AccountRequest>(req => req.HashedPassword == "$2a$13$hashed")), Times.Once);
    }

    // ── UpdateAccountAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAccountAsync_ReturnsFail_WhenAccountNotFound()
    {
        _accountRepo.Setup(r => r.GetAccountByEmailAsync("ghost@example.com")).ReturnsAsync((AccountResponse?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateAccountAsync(
            new AccountRequest { Email = "ghost@example.com" }, null);

        Assert.False(result.Success);
        Assert.Contains("wrong", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAccountAsync_KeepsExistingPassword_WhenNewPasswordIsEmpty()
    {
        var account = ActiveAccount();
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(account.Email!)).ReturnsAsync(account);
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<AccountRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateAccountAsync(
            new AccountRequest { Email = account.Email, Role = "User" }, null);

        Assert.True(result.Success);
        _accountRepo.Verify(r => r.UpdateAccountAsync(
            It.Is<AccountRequest>(req => req.HashedPassword == account.HashedPassword)), Times.Once);
        _passwordService.Verify(p => p.HashPassword(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAccountAsync_HashesNewPassword_WhenNewPasswordProvided()
    {
        var account = ActiveAccount();
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(account.Email!)).ReturnsAsync(account);
        _passwordService.Setup(p => p.HashPassword("newpass")).Returns("$2a$13$newhash");
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<AccountRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateAccountAsync(
            new AccountRequest { Email = account.Email, Role = "User" }, "newpass");

        Assert.True(result.Success);
        _accountRepo.Verify(r => r.UpdateAccountAsync(
            It.Is<AccountRequest>(req => req.HashedPassword == "$2a$13$newhash")), Times.Once);
    }

    [Fact]
    public async Task UpdateAccountAsync_ResetsLoginAttempt_WhenUnlocking()
    {
        var account = ActiveAccount();
        account.Locked = true;
        account.LoginAttempt = 5;
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(account.Email!)).ReturnsAsync(account);
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<AccountRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateAccountAsync(
            new AccountRequest { Email = account.Email, Locked = false }, null);

        Assert.True(result.Success);
        _accountRepo.Verify(r => r.UpdateAccountAsync(
            It.Is<AccountRequest>(req => req.Locked == false && req.LoginAttempt == 0)), Times.Once);
    }

    // ── DeleteAccountAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAccountAsync_ReturnsFail_WhenAccountNotFound()
    {
        _accountRepo.Setup(r => r.GetAccountByEmailAsync("ghost@example.com")).ReturnsAsync((AccountResponse?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAccountAsync("ghost@example.com");

        Assert.False(result.Success);
        Assert.Contains("find", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteAccountAsync_SetsDeletedFlag_WhenAccountFound()
    {
        var account = ActiveAccount();
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(account.Email!)).ReturnsAsync(account);
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<AccountRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAccountAsync(account.Email!);

        Assert.True(result.Success);
        _accountRepo.Verify(r => r.UpdateAccountAsync(
            It.Is<AccountRequest>(req => req.Deleted == true)), Times.Once);
    }
}
