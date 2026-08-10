using Maroik.Core.Contract.Misc.Constants;
using Maroik.Core.Domain.Account;
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

    /// <summary>Starts the Testcontainers PostgreSQL instance and creates the schema before each test.</summary>
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

    /// <summary>Disposes the database context and stops the PostgreSQL container after each test.</summary>
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

    /// <summary>Get all async returns all accounts from postgre SQL.</summary>
    [Fact]
    public async Task GetAllAsync_ReturnsAllAccounts_FromPostgreSQL()
    {
        await SeedAsync(
            MakeOrm("alice@example.com", "Alice"),
            MakeOrm("bob@example.com", "Bob"));

        List<Account> result = await _sut.GetAllAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
    }

    // -- CreateAccountAsync ---------------------------------------------------

    /// <summary>Create account async persists to postgre SQL and is retrievable.</summary>
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

        await _sut.CreateAsync(account, TestContext.Current.CancellationToken);
        _context.ChangeTracker.Clear();

        Account? retrieved = await _sut.FindByEmailAsync("newuser@example.com", TestContext.Current.CancellationToken);

        Assert.NotNull(retrieved);
        Assert.Equal("NewUser", retrieved.Nickname);
        Assert.Equal("Asia/Seoul", retrieved.TimeZone.Value);
    }

    // -- UpdateEntityAsync ---------------------------------------------------

    /// <summary>Update entity async changes are persisted in postgre SQL.</summary>
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

        await _sut.UpdateEntityAsync(updated, TestContext.Current.CancellationToken);
        _context.ChangeTracker.Clear();

        Account? result = await _sut.FindByEmailAsync("alice@example.com", TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(Role.Admin, result.Role);
        Assert.Equal("America/New_York", result.TimeZone.Value);
    }

    // -- Unique constraint ----------------------------------------------------

    /// <summary>Create account async throws db update exception, when email already exists.</summary>
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
            () => _sut.CreateAsync(duplicate, TestContext.Current.CancellationToken));
    }

    // -- Soft delete visibility -----------------------------------------------

    /// <summary>Find async with deleted filter excludes soft deleted accounts in postgre SQL.</summary>
    [Fact]
    public async Task FindAsync_WithDeletedFilter_ExcludesSoftDeletedAccounts_InPostgreSQL()
    {
        var deleted = MakeOrm("deleted@example.com", "Deleted");
        deleted.Deleted = true;

        await SeedAsync(
            MakeOrm("active@example.com", "Active"),
            deleted);

        // Repository has no implicit soft-delete filter; GetAllAsync returns all accounts.
        List<Account> result = await _sut.GetAllAsync(TestContext.Current.CancellationToken);

        Assert.DoesNotContain(result, a => a.Email.Value == "deleted@example.com" && !a.Deleted);
        Assert.Contains(result, a => a.Email.Value == "active@example.com");
    }

    // -- FindByEmailForUpdateAsync ------------------------------------------------

    /// <summary>Find by email for update async returns account, when exists.</summary>
    [Fact]
    public async Task FindByEmailForUpdateAsync_ReturnsAccount_WhenExists()
    {
        await SeedAsync(MakeOrm("alice@example.com", "Alice"));

        await using var unitOfWork = new UnitOfWork(_context);
        await unitOfWork.BeginAsync(TestContext.Current.CancellationToken);
        Account? result = await _sut.FindByEmailForUpdateAsync("alice@example.com", TestContext.Current.CancellationToken);
        await unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("Alice", result.Nickname);
    }

    /// <summary>Find by email for update async returns null, when not found.</summary>
    [Fact]
    public async Task FindByEmailForUpdateAsync_ReturnsNull_WhenNotFound()
    {
        await using var unitOfWork = new UnitOfWork(_context);
        await unitOfWork.BeginAsync(TestContext.Current.CancellationToken);
        Account? result = await _sut.FindByEmailForUpdateAsync("missing@example.com", TestContext.Current.CancellationToken);
        await unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that concurrent failed-login sequences for the same account all accumulate correctly
    /// against real PostgreSQL when each is wrapped in its own transaction around
    /// FindByEmailForUpdateAsync + UpdateEntityAsync (mirroring how AccountService.LoginAsync uses
    /// these together). The row lock serializes the concurrent transactions — each one blocks until
    /// the previous commits and then re-reads the current counter — instead of letting them race on a
    /// stale read the way a plain FindByEmailAsync + UpdateEntityAsync pair (with no lock) would.
    /// Each concurrent call uses its own <see cref="ApplicationDbContext"/> (a context instance is not
    /// thread-safe for concurrent operations) against the same database.
    /// </summary>
    [Fact]
    public async Task FindByEmailForUpdateAsync_SerializesConcurrentIncrements_AccumulatingCorrectly()
    {
        await SeedAsync(MakeOrm("alice@example.com", "Alice"));
        string connectionString = _postgres.GetConnectionString();

        IEnumerable<Task> tasks = Enumerable.Range(0, 5).Select(async _ =>
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connectionString).Options;
            await using var context = new ApplicationDbContext(options);
            var repo = new AccountRepository(context);
            await using var unitOfWork = new UnitOfWork(context);

            await unitOfWork.BeginAsync(TestContext.Current.CancellationToken);
            Account account = (await repo.FindByEmailForUpdateAsync("alice@example.com", TestContext.Current.CancellationToken))!;
            account.RecordLoginFailure(maxAttempts: 100);
            await repo.UpdateEntityAsync(account, TestContext.Current.CancellationToken);
            await unitOfWork.CommitAsync(TestContext.Current.CancellationToken);
        });
        await Task.WhenAll(tasks);

        _context.ChangeTracker.Clear();
        Account? updated = await _sut.FindByEmailAsync("alice@example.com", TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(5, updated.LoginAttempt);
    }
}
