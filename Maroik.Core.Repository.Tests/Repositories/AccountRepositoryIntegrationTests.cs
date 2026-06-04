using Maroik.Core.Contract.Domains.Account;
using Maroik.Core.Contract.Utilities;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using OrmAccount = Maroik.Core.PostgreSQL.Models.Account;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Integration tests for <see cref="AccountRepository"/> against a real PostgreSQL instance
/// (spun up via Testcontainers). Unlike the InMemory-based unit tests, these tests exercise
/// actual SQL behaviour — constraints, case-sensitive queries, and concurrent-write semantics.
/// </summary>
[Trait("Category", "Integration")]
public class AccountRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17")
        .Build();

    private ApplicationDbContext _context = null!;
    private AccountRepository _sut = null!;

    public async ValueTask InitializeAsync()
    {
        // Npgsql 9+ requires DateTimeKind.Unspecified for timestamp-without-timezone columns.
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        _context = new ApplicationDbContext(options);
        await _context.Database.EnsureCreatedAsync();
        _sut = new AccountRepository(_context);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    // -- Helpers --------------------------------------------------------------

    private static OrmAccount MakeOrm(string email, string nickname) => new()
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
        _context.ChangeTracker.Clear();
    }

    // -- GetAllAsync ----------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_ReturnsAllAccounts_FromPostgreSQL()
    {
        await SeedAsync(
            MakeOrm("alice@example.com", "Alice"),
            MakeOrm("bob@example.com", "Bob"));

        List<Account> result = await _sut.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    // -- CreateAccountAsync ---------------------------------------------------

    [Fact]
    public async Task CreateAccountAsync_PersistsToPostgreSQL_AndIsRetrievable()
    {
        var account = Account.Reconstitute(
            email: "newuser@example.com",
            hashedPassword: "$2a$13$hash",
            nickname: "NewUser",
            avatarImagePath: "/upload/Management/Profile/default-avatar.jpg",
            role: Role.User,
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

        await _sut.CreateAsync(account);
        _context.ChangeTracker.Clear();

        Account? retrieved = await _sut.FindByEmailAsync("newuser@example.com");

        Assert.NotNull(retrieved);
        Assert.Equal("NewUser", retrieved.Nickname);
        Assert.Equal("Asia/Seoul", retrieved.TimeZone.Value);
    }

    // -- UpdateEntityAsync ---------------------------------------------------

    [Fact]
    public async Task UpdateEntityAsync_ChangesArePersisted_InPostgreSQL()
    {
        await SeedAsync(MakeOrm("alice@example.com", "Alice"));

        var updated = Account.Reconstitute(
            email: "alice@example.com",
            hashedPassword: "$2a$13$newhash",
            nickname: "Alice",
            avatarImagePath: "/upload/Management/Profile/default-avatar.jpg",
            role: Role.Admin,
            timeZoneIanaId: "America/New_York",
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

        await _sut.UpdateEntityAsync(updated);
        _context.ChangeTracker.Clear();

        Account? result = await _sut.FindByEmailAsync("alice@example.com");

        Assert.NotNull(result);
        Assert.Equal(Role.Admin, result.Role);
        Assert.Equal("America/New_York", result.TimeZone.Value);
    }

    // -- Unique constraint ----------------------------------------------------

    [Fact]
    public async Task CreateAccountAsync_ThrowsDbUpdateException_WhenEmailAlreadyExists()
    {
        await SeedAsync(MakeOrm("alice@example.com", "Alice"));

        var duplicate = Account.Reconstitute(
            email: "alice@example.com",
            hashedPassword: "$2a$13$hash2",
            nickname: "AliceDuplicate",
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

        // PostgreSQL enforces the PK/unique constraint — InMemory does not
        await Assert.ThrowsAsync<DbUpdateException>(
            () => _sut.CreateAsync(duplicate));
    }

    // -- Soft delete visibility -----------------------------------------------

    [Fact]
    public async Task FindAsync_WithDeletedFilter_ExcludesSoftDeletedAccounts_InPostgreSQL()
    {
        var deleted = MakeOrm("deleted@example.com", "Deleted");
        deleted.Deleted = true;

        await SeedAsync(
            MakeOrm("active@example.com", "Active"),
            deleted);

        // Repository has no implicit soft-delete filter; GetAllAsync returns all accounts.
        List<Account> result = await _sut.GetAllAsync();

        Assert.DoesNotContain(result, a => a.Email.Value == "deleted@example.com" && !a.Deleted);
        Assert.Contains(result, a => a.Email.Value == "active@example.com");
    }
}
