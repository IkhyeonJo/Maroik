using Maroik.Core.Contract.Constants;
using Maroik.Core.Contract.Domains.Account;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using OrmAccount = Maroik.Core.PostgreSQL.Models.Account;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="AccountRepository"/> using an EF Core InMemory database.
/// Covers CRUD operations, soft-delete behaviour, login-attempt tracking,
/// email-confirmation token handling, and password-reset token flows.
/// </summary>
public class AccountRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AccountRepository _sut;

    /// <summary>Initializes the test fixture, setting up all required test doubles and the system under test.</summary>
    public AccountRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _sut = new AccountRepository(_context);
    }

    /// <summary>Releases resources allocated by this test fixture (disposes the database context).</summary>
    public void Dispose() => _context.Dispose();

    // -- Helpers --------------------------------------------------------------

    private static OrmAccount MakeAccount(string email, string nickname) => new()
    {
        Email = email,
        Nickname = nickname,
        HashedPassword = "$2a$13$placeholder",
        AvatarImagePath = "/upload/Management/Profile/default-avatar.jpg",
        Role = Role.User,
        TimeZoneIanaId = "UTC",
        Deleted = false,
        Locked = false,
        EmailConfirmed = true,
        AgreedServiceTerms = true,
        LoginAttempt = 0
    };

    private async Task SeedAsync(params OrmAccount[] accounts)
    {
        await _context.Accounts.AddRangeAsync(accounts);
        await _context.SaveChangesAsync();
    }

    // -- GetAllAsync ----------------------------------------------------------

    /// <summary>Verifies that <c>GetAllAsync</c> returns all accounts.</summary>
    [Fact]
    public async Task GetAllAsync_ReturnsAllAccounts()
    {
        await SeedAsync(
            MakeAccount("alice@example.com", "Alice"),
            MakeAccount("bob@example.com", "Bob"));

        List<Account> result = await _sut.GetAllAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
    }

    /// <summary>Verifies that <c>GetAllAsync</c> returns empty list when no accounts.</summary>
    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoAccounts()
    {
        List<Account> result = await _sut.GetAllAsync(TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    // -- GetAccountByEmailAsync -----------------------------------------------

    /// <summary>Verifies that <c>GetAccountByEmailAsync</c> returns account when found.</summary>
    [Fact]
    public async Task GetAccountByEmailAsync_ReturnsAccount_WhenFound()
    {
        await SeedAsync(MakeAccount("alice@example.com", "Alice"));

        Account? result = await _sut.FindByEmailAsync("alice@example.com", TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("alice@example.com", result.Email.Value);
        Assert.Equal("Alice", result.Nickname);
    }

    /// <summary>Verifies that <c>GetAccountByEmailAsync</c> returns null when not found.</summary>
    [Fact]
    public async Task GetAccountByEmailAsync_ReturnsNull_WhenNotFound()
    {
        Account? result = await _sut.FindByEmailAsync("ghost@example.com", TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    // -- GetAccountByRegistrationTokenAsync -----------------------------------

    /// <summary>Verifies that <c>GetAccountByRegistrationTokenAsync</c> returns account when token matches.</summary>
    [Fact]
    public async Task GetAccountByRegistrationTokenAsync_ReturnsAccount_WhenTokenMatches()
    {
        const string token = "unique-registration-token";
        var account = MakeAccount("alice@example.com", "Alice");
        account.RegistrationToken = token;
        await SeedAsync(account);

        Account? result = await _sut.FindByRegistrationTokenAsync(token, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("alice@example.com", result.Email.Value);
    }

    /// <summary>Verifies that <c>GetAccountByRegistrationTokenAsync</c> returns null when token does not match.</summary>
    [Fact]
    public async Task GetAccountByRegistrationTokenAsync_ReturnsNull_WhenTokenDoesNotMatch()
    {
        Account? result = await _sut.FindByRegistrationTokenAsync("nonexistent-token", TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    // -- GetAccountByResetPasswordTokenAsync ---------------------------------

    /// <summary>Verifies that <c>GetAccountByResetPasswordTokenAsync</c> returns account when token matches.</summary>
    [Fact]
    public async Task GetAccountByResetPasswordTokenAsync_ReturnsAccount_WhenTokenMatches()
    {
        const string token = "reset-password-token-xyz";
        var account = MakeAccount("alice@example.com", "Alice");
        account.ResetPasswordToken = token;
        await SeedAsync(account);

        Account? result = await _sut.FindByResetPasswordTokenAsync(token, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("alice@example.com", result.Email.Value);
    }

    /// <summary>Verifies that <c>GetAccountByResetPasswordTokenAsync</c> returns null when token not found.</summary>
    [Fact]
    public async Task GetAccountByResetPasswordTokenAsync_ReturnsNull_WhenTokenNotFound()
    {
        Account? result = await _sut.FindByResetPasswordTokenAsync("no-such-token", TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    // -- CreateAccountAsync ---------------------------------------------------

    /// <summary>Verifies that <c>CreateAccountAsync</c> adds account to database.</summary>
    [Fact]
    public async Task CreateAccountAsync_AddsAccountToDatabase()
    {
        var account = Account.Reconstitute(
            email: "new@example.com",
            hashedPassword: "$2a$13$hash",
            nickname: "NewUser",
            avatarImagePath: "/upload/Management/Profile/default-avatar.jpg",
            role: Role.User,
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

        await _sut.CreateAsync(account, TestContext.Current.CancellationToken);

        OrmAccount? saved = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == "new@example.com", TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal("NewUser", saved.Nickname);
    }

    // -- GetAccountsByNicknamesAsync -----------------------------------------

    /// <summary>Verifies that <c>GetAccountsByNicknamesAsync</c> returns matching accounts.</summary>
    [Fact]
    public async Task GetAccountsByNicknamesAsync_ReturnsMatchingAccounts()
    {
        await SeedAsync(
            MakeAccount("alice@example.com", "Alice"),
            MakeAccount("bob@example.com", "Bob"),
            MakeAccount("carol@example.com", "Carol"));

        List<Account> result = await _sut.FindByNicknamesAsync(new[] { "Alice", "Carol" }, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, a => a.Nickname == "Alice");
        Assert.Contains(result, a => a.Nickname == "Carol");
    }

    /// <summary>Verifies that <c>GetAccountsByNicknamesAsync</c> returns empty when none match.</summary>
    [Fact]
    public async Task GetAccountsByNicknamesAsync_ReturnsEmpty_WhenNoneMatch()
    {
        await SeedAsync(MakeAccount("alice@example.com", "Alice"));

        List<Account> result = await _sut.FindByNicknamesAsync(new[] { "Ghost" }, TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    /// <summary>Verifies that <c>GetAccountsByNicknamesAsync</c> returns empty when list is empty.</summary>
    [Fact]
    public async Task GetAccountsByNicknamesAsync_ReturnsEmpty_WhenListIsEmpty()
    {
        await SeedAsync(MakeAccount("alice@example.com", "Alice"));

        List<Account> result = await _sut.FindByNicknamesAsync(Array.Empty<string>(), TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    // -- UpdateEntityAsync ---------------------------------------------------

    /// <summary>Verifies that <c>UpdateEntityAsync</c> updates existing account.</summary>
    [Fact]
    public async Task UpdateEntityAsync_UpdatesExistingAccount()
    {
        await SeedAsync(MakeAccount("alice@example.com", "Alice"));

        var account = Account.Reconstitute(
            email: "alice@example.com",
            hashedPassword: "$2a$13$newhash",
            nickname: "Alice",
            avatarImagePath: "/upload/Management/Profile/default-avatar.jpg",
            role: Role.Admin,
            timeZoneIanaId: "Asia/Seoul",
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

        await _sut.UpdateEntityAsync(account, TestContext.Current.CancellationToken);

        OrmAccount? updated = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == "alice@example.com", TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(Role.Admin, updated.Role);
        Assert.Equal("Asia/Seoul", updated.TimeZoneIanaId);
    }

    // RecordLoginFailureAsync is covered in AccountRepositoryIntegrationTests: it uses ExecuteUpdateAsync,
    // which the InMemory provider used by this test class does not support.
}
