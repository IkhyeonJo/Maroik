using Maroik.Core.Contract.Dtos;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Tests.Repositories;

public class AccountRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AccountRepository _sut;

    public AccountRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _sut = new AccountRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Account MakeAccount(string email, string nickname) => new()
    {
        Email = email,
        Nickname = nickname,
        HashedPassword = "$2a$13$placeholder",
        AvatarImagePath = "/upload/Management/Profile/default-avatar.jpg",
        Role = "User",
        TimeZoneIanaId = "UTC",
        Deleted = false,
        Locked = false,
        EmailConfirmed = true,
        AgreedServiceTerms = true,
        LoginAttempt = 0
    };

    private async Task SeedAsync(params Account[] accounts)
    {
        await _context.Accounts.AddRangeAsync(accounts);
        await _context.SaveChangesAsync();
    }

    // ── GetAllAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllAccounts()
    {
        await SeedAsync(
            MakeAccount("alice@example.com", "Alice"),
            MakeAccount("bob@example.com", "Bob"));

        List<AccountResponse> result = await _sut.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoAccounts()
    {
        List<AccountResponse> result = await _sut.GetAllAsync();

        Assert.Empty(result);
    }

    // ── GetAccountByEmailAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetAccountByEmailAsync_ReturnsAccount_WhenFound()
    {
        await SeedAsync(MakeAccount("alice@example.com", "Alice"));

        AccountResponse? result = await _sut.GetAccountByEmailAsync("alice@example.com");

        Assert.NotNull(result);
        Assert.Equal("alice@example.com", result.Email);
        Assert.Equal("Alice", result.Nickname);
    }

    [Fact]
    public async Task GetAccountByEmailAsync_ReturnsNull_WhenNotFound()
    {
        AccountResponse? result = await _sut.GetAccountByEmailAsync("ghost@example.com");

        Assert.Null(result);
    }

    // ── GetAccountByRegistrationTokenAsync ───────────────────────────────────

    [Fact]
    public async Task GetAccountByRegistrationTokenAsync_ReturnsAccount_WhenTokenMatches()
    {
        const string token = "unique-registration-token";
        var account = MakeAccount("alice@example.com", "Alice");
        account.RegistrationToken = token;
        await SeedAsync(account);

        AccountResponse? result = await _sut.GetAccountByRegistrationTokenAsync(token);

        Assert.NotNull(result);
        Assert.Equal("alice@example.com", result.Email);
    }

    [Fact]
    public async Task GetAccountByRegistrationTokenAsync_ReturnsNull_WhenTokenDoesNotMatch()
    {
        AccountResponse? result = await _sut.GetAccountByRegistrationTokenAsync("nonexistent-token");

        Assert.Null(result);
    }

    // ── GetAccountByResetPasswordTokenAsync ─────────────────────────────────

    [Fact]
    public async Task GetAccountByResetPasswordTokenAsync_ReturnsAccount_WhenTokenMatches()
    {
        const string token = "reset-password-token-xyz";
        var account = MakeAccount("alice@example.com", "Alice");
        account.ResetPasswordToken = token;
        await SeedAsync(account);

        AccountResponse? result = await _sut.GetAccountByResetPasswordTokenAsync(token);

        Assert.NotNull(result);
        Assert.Equal("alice@example.com", result.Email);
    }

    [Fact]
    public async Task GetAccountByResetPasswordTokenAsync_ReturnsNull_WhenTokenNotFound()
    {
        AccountResponse? result = await _sut.GetAccountByResetPasswordTokenAsync("no-such-token");

        Assert.Null(result);
    }

    // ── CreateAccountAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateAccountAsync_AddsAccountToDatabase()
    {
        var request = new AccountRequest
        {
            Email = "new@example.com",
            HashedPassword = "$2a$13$hash",
            Nickname = "NewUser",
            AvatarImagePath = "/upload/Management/Profile/default-avatar.jpg",
            Role = "User",
            TimeZoneIanaId = "UTC"
        };

        await _sut.CreateAccountAsync(request);

        Account? saved = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == "new@example.com");
        Assert.NotNull(saved);
        Assert.Equal("NewUser", saved.Nickname);
    }

    // ── UpdateAccountAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAccountAsync_UpdatesExistingAccount()
    {
        await SeedAsync(MakeAccount("alice@example.com", "Alice"));

        var request = new AccountRequest
        {
            Email = "alice@example.com",
            HashedPassword = "$2a$13$newhash",
            Nickname = "Alice",
            AvatarImagePath = "/upload/Management/Profile/default-avatar.jpg",
            Role = "Admin",
            TimeZoneIanaId = "Asia/Seoul",
            Locked = false,
            LoginAttempt = 0,
            EmailConfirmed = true,
            AgreedServiceTerms = true
        };

        await _sut.UpdateAccountAsync(request);

        Account? updated = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == "alice@example.com");
        Assert.NotNull(updated);
        Assert.Equal("Admin", updated.Role);
        Assert.Equal("Asia/Seoul", updated.TimeZoneIanaId);
    }
}
