using Maroik.Core.Domain.Account;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using OrmAccount = Maroik.Core.PostgreSQL.Models.Account;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="AccountRepository"/> against a real PostgreSQL database (Testcontainers)
/// pre-loaded with the production <c>Init.sql</c> schema and seed data. Exercises real SQL
/// behaviour the EF Core InMemory provider could not: the <c>Account_Nickname_unique</c> index,
/// the case-sensitive <c>=</c> lookup, the <c>Account_Role_check</c> constraint, column defaults
/// and <c>SELECT ... FOR UPDATE</c> row-lock semantics.
/// Each test inserts under keys unique to itself (<see cref="RepositoryTestBase.UniqueEmail"/> /
/// <see cref="RepositoryTestBase.Unique"/>) so it never collides with the seed or a sibling test.
/// </summary>
public sealed class AccountRepositoryTests(DatabaseFixture database) : RepositoryTestBase(database)
{
    private AccountRepository Sut => new(Context);

    // -- Helpers --------------------------------------------------------------

    private OrmAccount NewAccount(
        string? email = null,
        string? nickname = null,
        bool deleted = false,
        bool locked = false,
        long loginAttempt = 0,
        string role = Role.User,
        string? registrationToken = null,
        string? resetPasswordToken = null) => new()
    {
        Email = email ?? UniqueEmail("a" + Guid.NewGuid().ToString("N")[..8]),
        Nickname = nickname ?? Unique("Nick-" + Guid.NewGuid().ToString("N")[..8]),
        HashedPassword = "$2a$13$placeholder",
        AvatarImagePath = "/upload/Management/Profile/default-avatar.jpg",
        Role = role,
        TimeZoneIanaId = "UTC",
        DefaultMonetaryUnit = null,
        Deleted = deleted,
        Locked = locked,
        EmailConfirmed = true,
        AgreedServiceTerms = true,
        LoginAttempt = loginAttempt,
        RegistrationToken = registrationToken,
        ResetPasswordToken = resetPasswordToken,
        Created = DateTime.UtcNow,
        Updated = DateTime.UtcNow,
        SecurityStamp = "stamp"
    };

    private Account NewDomainAccount(string email, string nickname, string role = Role.User, string timeZone = "UTC") =>
        Account.Reconstitute(
            email: email, hashedPassword: "$2a$13$hash", nickname: nickname, avatarImagePath: null,
            role: role, timeZoneIanaId: timeZone, defaultMonetaryUnit: null, locked: false, loginAttempt: 0,
            emailConfirmed: true, agreedServiceTerms: true, registrationToken: null, resetPasswordToken: null,
            created: DateTime.UtcNow, updated: DateTime.UtcNow, message: null, deleted: false,
            securityStamp: "stamp", mustChangePassword: false);

    private async Task SeedAsync(params OrmAccount[] accounts)
    {
        await Context.Accounts.AddRangeAsync(accounts);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
    }

    // -- GetAllAsync --------------------------------------------------------------

    /// <summary>Verifies that <c>GetAllAsync</c> returns every account row this test inserted.</summary>
    [Fact]
    public async Task GetAllAsync_ReturnsInsertedAccounts()
    {
        string a = UniqueEmail("alice");
        string b = UniqueEmail("bob");
        await SeedAsync(NewAccount(a, Unique("Alice")), NewAccount(b, Unique("Bob")));

        List<Account> result = await Sut.GetAllAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count(x => x.Email.Value == a || x.Email.Value == b));
    }

    /// <summary>Verifies that <c>GetAllAsync</c> also returns the accounts already present in the seed data.</summary>
    [Fact]
    public async Task GetAllAsync_IncludesSeededAccounts()
    {
        List<Account> result = await Sut.GetAllAsync(TestContext.Current.CancellationToken);

        Assert.Contains(result, x => x.Email.Value == "admin@maroik.com" && x.Role == Role.Admin);
        Assert.Contains(result, x => x.Email.Value == "user@maroik.com");
    }

    /// <summary>Repository applies no implicit soft-delete filter: a seeded, deleted account is still returned.</summary>
    [Fact]
    public async Task GetAllAsync_ReturnsSoftDeletedAccounts_NotFiltered()
    {
        var deleted = NewAccount(UniqueEmail("gone"), Unique("Gone"), deleted: true);
        await SeedAsync(deleted);

        List<Account> result = await Sut.GetAllAsync(TestContext.Current.CancellationToken);

        Assert.Contains(result, x => x.Email.Value == deleted.Email && x.Deleted);
        // The seed's own soft-deleted account (maroik@maroik.com) proves the point independently.
        Assert.Contains(result, x => x.Email.Value == "maroik@maroik.com" && x.Deleted);
    }

    // -- FindByEmailAsync -------------------------------------------------------

    /// <summary>Verifies that <c>FindByEmailAsync</c> returns the account when found.</summary>
    [Fact]
    public async Task FindByEmailAsync_ReturnsAccount_WhenFound()
    {
        string email = UniqueEmail("alice");
        await SeedAsync(NewAccount(email, Unique("Alice")));

        Account? result = await Sut.FindByEmailAsync(email, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(email, result.Email.Value);
    }

    /// <summary>Verifies that <c>FindByEmailAsync</c> returns null when not found.</summary>
    [Fact]
    public async Task FindByEmailAsync_ReturnsNull_WhenNotFound()
    {
        Account? result = await Sut.FindByEmailAsync(UniqueEmail("ghost"), TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    /// <summary>
    /// Email is always persisted lower-case (see Email.Create), but a lookup can arrive with a
    /// different case (e.g. a login form auto-capitalized by the browser). Against PostgreSQL's
    /// case-sensitive <c>=</c> it must still match, or a user who registered as "Foo@Bar.com"
    /// (stored "foo@bar.com") is locked out re-typing the same case at login.
    /// </summary>
    [Fact]
    public async Task FindByEmailAsync_IsCaseInsensitive()
    {
        string email = UniqueEmail("alice");
        await SeedAsync(NewAccount(email, Unique("Alice")));

        Account? result = await Sut.FindByEmailAsync(email.ToUpperInvariant(), TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(email, result.Email.Value);
    }

    // -- FindByRegistrationTokenAsync / FindByResetPasswordTokenAsync ----------

    /// <summary>Verifies that <c>FindByRegistrationTokenAsync</c> returns the account when the token matches.</summary>
    [Fact]
    public async Task FindByRegistrationTokenAsync_ReturnsAccount_WhenTokenMatches()
    {
        string token = Unique("reg-token");
        await SeedAsync(NewAccount(registrationToken: token));

        Account? result = await Sut.FindByRegistrationTokenAsync(token, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
    }

    /// <summary>Verifies that <c>FindByRegistrationTokenAsync</c> returns null when no token matches.</summary>
    [Fact]
    public async Task FindByRegistrationTokenAsync_ReturnsNull_WhenTokenDoesNotMatch()
    {
        Account? result = await Sut.FindByRegistrationTokenAsync(Unique("nope"), TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    /// <summary>Verifies that <c>FindByResetPasswordTokenAsync</c> returns the account when the token matches.</summary>
    [Fact]
    public async Task FindByResetPasswordTokenAsync_ReturnsAccount_WhenTokenMatches()
    {
        string token = Unique("reset-token");
        await SeedAsync(NewAccount(resetPasswordToken: token));

        Account? result = await Sut.FindByResetPasswordTokenAsync(token, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
    }

    /// <summary>Verifies that <c>FindByResetPasswordTokenAsync</c> returns null when the token is not found.</summary>
    [Fact]
    public async Task FindByResetPasswordTokenAsync_ReturnsNull_WhenTokenNotFound()
    {
        Account? result = await Sut.FindByResetPasswordTokenAsync(Unique("no-such"), TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    // -- FindByNicknamesAsync -------------------------------------------------

    /// <summary>Verifies that <c>FindByNicknamesAsync</c> returns exactly the matching accounts.</summary>
    [Fact]
    public async Task FindByNicknamesAsync_ReturnsMatchingAccounts()
    {
        string n1 = Unique("Alice");
        string n2 = Unique("Carol");
        await SeedAsync(
            NewAccount(nickname: n1),
            NewAccount(nickname: Unique("Bob")),
            NewAccount(nickname: n2));

        List<Account> result = await Sut.FindByNicknamesAsync(new[] { n1, n2 }, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, a => a.Nickname == n1);
        Assert.Contains(result, a => a.Nickname == n2);
    }

    /// <summary>Verifies that <c>FindByNicknamesAsync</c> returns empty when none match.</summary>
    [Fact]
    public async Task FindByNicknamesAsync_ReturnsEmpty_WhenNoneMatch()
    {
        List<Account> result = await Sut.FindByNicknamesAsync(new[] { Unique("Ghost") }, TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    /// <summary>Verifies that <c>FindByNicknamesAsync</c> returns empty when the name list is empty.</summary>
    [Fact]
    public async Task FindByNicknamesAsync_ReturnsEmpty_WhenListIsEmpty()
    {
        List<Account> result = await Sut.FindByNicknamesAsync(Array.Empty<string>(), TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    // -- CreateAsync --------------------------------------------------------------

    /// <summary>Verifies that <c>CreateAsync</c> persists a new account row that is then retrievable.</summary>
    [Fact]
    public async Task CreateAsync_PersistsAccount_AndIsRetrievable()
    {
        string email = UniqueEmail("new");
        await Sut.CreateAsync(NewDomainAccount(email, Unique("NewUser"), timeZone: "Asia/Seoul"), TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        Account? retrieved = await Sut.FindByEmailAsync(email, TestContext.Current.CancellationToken);

        Assert.NotNull(retrieved);
        Assert.Equal("Asia/Seoul", retrieved.TimeZone.Value);
    }

    /// <summary>PostgreSQL enforces the <c>Account</c> primary key — a duplicate e-mail throws (InMemory did not).</summary>
    [Fact]
    public async Task CreateAsync_Throws_WhenEmailAlreadyExists()
    {
        string email = UniqueEmail("dup");
        await SeedAsync(NewAccount(email, Unique("First")));

        await Assert.ThrowsAsync<DbUpdateException>(
            () => Sut.CreateAsync(NewDomainAccount(email, Unique("Second")), TestContext.Current.CancellationToken));
    }

    /// <summary>PostgreSQL enforces <c>Account_Nickname_unique</c> — a duplicate nickname throws (InMemory did not).</summary>
    [Fact]
    public async Task CreateAsync_Throws_WhenNicknameAlreadyExists()
    {
        string nickname = Unique("Twin");
        await SeedAsync(NewAccount(nickname: nickname));

        await Assert.ThrowsAsync<DbUpdateException>(
            () => Sut.CreateAsync(NewDomainAccount(UniqueEmail("other"), nickname), TestContext.Current.CancellationToken));
    }

    // -- UpdateEntityAsync ---------------------------------------------------

    /// <summary>Verifies that <c>UpdateEntityAsync</c> persists changes to an existing account.</summary>
    [Fact]
    public async Task UpdateEntityAsync_UpdatesExistingAccount()
    {
        string email = UniqueEmail("alice");
        await SeedAsync(NewAccount(email, Unique("Alice")));

        Account updated = NewDomainAccount(email, Unique("Alice"), role: Role.Admin, timeZone: "Asia/Seoul");
        await Sut.UpdateEntityAsync(updated, TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        Account? result = await Sut.FindByEmailAsync(email, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(Role.Admin, result.Role);
        Assert.Equal("Asia/Seoul", result.TimeZone.Value);
    }

    /// <summary>Verifies that <c>UpdateEntityAsync</c> throws, rather than silently no-op'ing, when the row no longer exists.</summary>
    [Fact]
    public async Task UpdateEntityAsync_Throws_WhenAccountNoLongerExists()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Sut.UpdateEntityAsync(NewDomainAccount(UniqueEmail("ghost"), Unique("Ghost")), TestContext.Current.CancellationToken));
    }

    // -- Column-scoped updates ----------------------------------------------------

    /// <summary>Each column-scoped update writes only its own columns and leaves every other column intact.</summary>
    [Fact]
    public async Task ColumnScopedUpdates_TouchOnlyTheirOwnColumns()
    {
        string email = UniqueEmail("scoped");
        var seed = NewAccount(email, Unique("Scoped"));
        seed.AvatarImagePath = "/original-avatar.jpg";
        seed.TimeZoneIanaId = "UTC";
        seed.HashedPassword = "$2a$13$original";
        seed.SecurityStamp = "original-stamp";
        seed.MustChangePassword = false;
        seed.ResetPasswordToken = "pending-reset-token";
        seed.DefaultMonetaryUnit = "USD";
        seed.Message = "original-message";
        await SeedAsync(seed);

        var ct = TestContext.Current.CancellationToken;
        var t1 = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        int n1 = await Sut.UpdateAvatarPathAsync(email, "/new-avatar.jpg", t1, ct);
        int n2 = await Sut.UpdateTimeZoneAsync(email, "Asia/Seoul", t1, ct);
        int n3 = await Sut.UpdatePasswordAsync(email, "$2a$13$new", "new-stamp", mustChangePassword: true, resetPasswordToken: null, t1, ct);
        int n4 = await Sut.UpdateDefaultMonetaryUnitAsync(email, "KRW", ct);
        int n5 = await Sut.UpdateMessageAsync(email, "new-message", t1, ct);
        Context.ChangeTracker.Clear();

        Assert.Equal(1, n1);
        Assert.Equal(1, n2);
        Assert.Equal(1, n3);
        Assert.Equal(1, n4);
        Assert.Equal(1, n5);

        Account? result = await Sut.FindByEmailAsync(email, ct);
        Assert.NotNull(result);
        Assert.Equal("/new-avatar.jpg", result.AvatarImagePath);
        Assert.Equal("Asia/Seoul", result.TimeZone.Value);
        Assert.Equal("$2a$13$new", result.HashedPassword);
        Assert.Equal("new-stamp", result.SecurityStamp);
        Assert.True(result.MustChangePassword);
        // A password write discards the pending reset token, so a link mailed earlier is dead.
        Assert.Null(result.ResetPasswordToken);
        Assert.Equal("KRW", result.DefaultMonetaryUnit);
        Assert.Equal("new-message", result.Message);
        // Untouched by any of the above.
        Assert.True(result.EmailConfirmed);
        Assert.Equal(Role.User, result.Role);
    }

    /// <summary>A null avatar path persists as the shared default-avatar path, matching ToEntity.</summary>
    [Fact]
    public async Task UpdateAvatarPathAsync_NullPersistsAsDefaultAvatar()
    {
        string email = UniqueEmail("nullavatar");
        var seed = NewAccount(email, Unique("NullAvatar"));
        seed.AvatarImagePath = "/some-custom.jpg";
        await SeedAsync(seed);

        int n = await Sut.UpdateAvatarPathAsync(email, null, DateTime.UtcNow, TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        Assert.Equal(1, n);
        Account? result = await Sut.FindByEmailAsync(email, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(Account.DefaultAvatarImagePath, result.AvatarImagePath);
    }

    /// <summary>A column-scoped update against a non-existent account affects zero rows (no throw).</summary>
    [Fact]
    public async Task ColumnScopedUpdate_ReturnsZero_WhenAccountMissing()
    {
        int n = await Sut.UpdateMessageAsync(UniqueEmail("ghost"), "x", DateTime.UtcNow, TestContext.Current.CancellationToken);
        Assert.Equal(0, n);
    }

    // -- FindByEmailForUpdateAsync (raw SELECT ... FOR UPDATE) ----------------

    /// <summary>Verifies that the row-locking lookup returns the account when it exists.</summary>
    [Fact]
    public async Task FindByEmailForUpdateAsync_ReturnsAccount_WhenExists()
    {
        string email = UniqueEmail("alice");
        string nickname = Unique("Alice");
        await SeedAsync(NewAccount(email, nickname));

        await using var unitOfWork = new UnitOfWork(Context);
        await unitOfWork.BeginAsync(TestContext.Current.CancellationToken);
        Account? result = await Sut.FindByEmailForUpdateAsync(email, TestContext.Current.CancellationToken);
        await unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(nickname, result.Nickname);
    }

    /// <summary>Verifies that the row-locking lookup returns null when the account is not found.</summary>
    [Fact]
    public async Task FindByEmailForUpdateAsync_ReturnsNull_WhenNotFound()
    {
        await using var unitOfWork = new UnitOfWork(Context);
        await unitOfWork.BeginAsync(TestContext.Current.CancellationToken);
        Account? result = await Sut.FindByEmailForUpdateAsync(UniqueEmail("missing"), TestContext.Current.CancellationToken);
        await unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    /// <summary>The FOR UPDATE login lookup must also match a differently-cased address (see <see cref="FindByEmailAsync_IsCaseInsensitive"/>).</summary>
    [Fact]
    public async Task FindByEmailForUpdateAsync_IsCaseInsensitive()
    {
        string email = UniqueEmail("alice");
        await SeedAsync(NewAccount(email, Unique("Alice")));

        await using var unitOfWork = new UnitOfWork(Context);
        await unitOfWork.BeginAsync(TestContext.Current.CancellationToken);
        Account? result = await Sut.FindByEmailForUpdateAsync(email.ToUpperInvariant(), TestContext.Current.CancellationToken);
        await unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(result);
    }

    /// <summary>
    /// Concurrent failed-login sequences for the same account, each wrapped in its own transaction
    /// around <c>FindByEmailForUpdateAsync</c> + <c>UpdateEntityAsync</c> (mirroring
    /// <c>AccountService.LoginAsync</c>). The row lock serializes the transactions — each blocks
    /// until the prior commits, then re-reads the current counter — so all five increments survive
    /// instead of racing on a stale read.
    /// </summary>
    [Fact]
    public async Task FindByEmailForUpdateAsync_SerializesConcurrentIncrements_AccumulatingCorrectly()
    {
        string email = UniqueEmail("race");
        await SeedAsync(NewAccount(email, Unique("Race")));

        // Contexts created up front: RepositoryTestBase tracks them for disposal and the tracking
        // list is not safe to append from parallel tasks.
        ApplicationDbContext[] contexts = [.. Enumerable.Range(0, 5).Select(_ => NewDbContext())];

        await Task.WhenAll(contexts.Select(async context =>
        {
            var repo = new AccountRepository(context);
            await using var unitOfWork = new UnitOfWork(context);
            await unitOfWork.BeginAsync(TestContext.Current.CancellationToken);
            Account account = (await repo.FindByEmailForUpdateAsync(email, TestContext.Current.CancellationToken))!;
            account.RecordLoginFailure(maxAttempts: 100);
            await repo.UpdateEntityAsync(account, TestContext.Current.CancellationToken);
            await unitOfWork.CommitAsync(TestContext.Current.CancellationToken);
        }));

        Context.ChangeTracker.Clear();
        Account? updated = await Sut.FindByEmailAsync(email, TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(5, updated.LoginAttempt);
    }
}
