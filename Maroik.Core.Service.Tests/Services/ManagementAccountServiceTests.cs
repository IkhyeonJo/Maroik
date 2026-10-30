using Maroik.Core.Contract.Dtos;
using Maroik.Core.Domain.Accounts;
using Maroik.Core.Service.Services;
using Maroik.Core.Service.Tests.Fixtures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ManagementAccountService"/>.
/// All repository and password-service dependencies are replaced with hand-written fakes.
/// Covers admin-side account management: list retrieval, create, update,
/// password reset, lock/unlock, and delete operations.
/// </summary>
public class ManagementAccountServiceTests : IAsyncDisposable
{
    /// <summary>The signed-in administrator performing the change (recorded in the audit log).</summary>
    private const string Actor = "admin@example.com";

    /// <summary>Fake <c>IAccountRepository</c> injected into the system under test.</summary>
    private readonly FakeAccountRepository _accountRepo = new();
    /// <summary>Fake <c>IPasswordService</c> injected into the system under test.</summary>
    private readonly FakePasswordService _passwordService = new();
    /// <summary>Fake <c>IUnitOfWork</c> injected into the system under test.</summary>
    private readonly FakeUnitOfWork _unitOfWork = new();
    /// <summary>Captures the log entries the system under test writes.</summary>
    private readonly FakeLogger<ManagementAccountService> _logger = new();

    /// <summary>
    /// No active administrator by default; the admin-safeguard tests set
    /// their own.
    /// </summary>
    public ManagementAccountServiceTests() =>
        _accountRepo.AnswerFindActiveAdminsForUpdateAsync(_ => Task.FromResult<List<Account>>([]));

    /// <summary>The fixed "current time" of these tests.</summary>
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
    /// <summary>The clock the service under test reads, stopped at <see cref="Now"/>.</summary>
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(Now));

    /// <summary>The service under test over the fake dependencies.</summary>
    private ManagementAccountService CreateSut() => new(
        _accountRepo,
        _passwordService,
        _unitOfWork,
        _logger,
        _time);

    // -- Helpers --------------------------------------------------------------

    /// <summary>A persisted account whose state is set by the arguments.</summary>
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
            deleted: false,
            securityStamp: "stamp",
            mustChangePassword: false);

    // -- GetAllAccountsAsync --------------------------------------------------

    /// <summary>Verifies that <c>GetAllAccountsAsync</c> delegates to repository.</summary>
    [Fact]
    public async Task GetAllAccountsAsync_DelegatesToRepository()
    {
        _accountRepo.AnswerGetAllAsync(_ => Task.FromResult<List<Account>>([ActiveAccount()]));
        var sut = CreateSut();

        var result = await sut.GetAllAccountsAsync(TestContext.Current.CancellationToken);

        Assert.Single(result);
    }

    /// <summary>
    /// The names of the secret account columns that no admin-grid row may carry: a live registration /
    /// reset token shown on the admin screen hands the account to whoever sees it, and a hash is material
    /// for an offline cracking attempt.
    /// </summary>
    private static readonly string[] SecretColumns = ["HashedPassword", "RegistrationToken", "ResetPasswordToken"];

    /// <summary>The admin grid's rows (all accounts) carry no password hash and no registration / reset token.</summary>
    [Fact]
    public async Task GetAllAccountsAsync_RowsCarryNoHashOrTokens()
    {
        _accountRepo.AnswerGetAllAsync(_ => Task.FromResult<List<Account>>([ActiveAccount()]));

        var row = Assert.Single(await CreateSut().GetAllAccountsAsync(TestContext.Current.CancellationToken));

        Assert.All(SecretColumns, name => Assert.Null(row.GetType().GetProperty(name)));
    }

    // -- GetAccountByEmailAsync -----------------------------------------------

    /// <summary>Verifies that <c>GetAccountByEmailAsync</c> returns null when not found.</summary>
    [Fact]
    public async Task GetAccountByEmailAsync_ReturnsNull_WhenNotFound()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));
        var sut = CreateSut();

        AccountResponse? result = await sut.GetAccountByEmailAsync("ghost@example.com", TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    /// <summary>
    /// The single-account lookup behind the admin edit form carries no password hash and no
    /// registration / reset token.
    /// </summary>
    [Fact]
    public async Task GetAccountByEmailAsync_ReturnsPlainAccountResponse_WithoutSecretColumns()
    {
        var account = ActiveAccount();
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(account));
        var sut = CreateSut();

        AccountResponse? result = await sut.GetAccountByEmailAsync(account.Email.Value, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(account.Email.Value, result.Email);
        Assert.All(SecretColumns, name => Assert.Null(result.GetType().GetProperty(name)));
    }

    // -- CreateAccountAsync ---------------------------------------------------

    /// <summary>Verifies that <c>CreateAccountAsync</c> returns fail when email already exists.</summary>
    [Fact]
    public async Task CreateAccountAsync_ReturnsFail_WhenEmailAlreadyExists()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(ActiveAccount("existing@example.com")));
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAccountAsync(new AdminCreateAccountRequest
        {
            Email = "existing@example.com",
            PlainPassword = "plain"
        }, Actor, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("already been created", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>CreateAccountAsync</c> hashes password and creates account when email is new.</summary>
    [Fact]
    public async Task CreateAccountAsync_HashesPasswordAndCreatesAccount_WhenEmailIsNew()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));
        _passwordService.AnswerHashPassword(password => password == "Plain1234!", _ => "$2a$13$hashedvalue");
        _accountRepo.AnswerCreateAsync((_, _) => Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAccountAsync(new AdminCreateAccountRequest
        {
            Email = "new@example.com",
            PlainPassword = "Plain1234!",
            Nickname = "NewUser",
            EmailConfirmed = true
        }, Actor, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Single(_accountRepo.CreateAsyncCalls, call => (call.Domain!.HashedPassword == "$2a$13$hashedvalue"));
    }

    /// <summary>An administrator may create an account under a reserved nickname (e.g. a second staff account).</summary>
    [Fact]
    public async Task CreateAccountAsync_AllowsReservedNickname_ForAdministrator()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));
        _passwordService.AnswerHashPassword(_ => "$2a$13$hashed");
        _accountRepo.AnswerCreateAsync((_, _) => Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAccountAsync(new AdminCreateAccountRequest
        {
            Email = "staff@example.com",
            PlainPassword = "Plain1234!",
            Nickname = "Admin",
            Role = Role.Admin,
            EmailConfirmed = true
        }, Actor, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Single(_accountRepo.CreateAsyncCalls, call => (call.Domain!.Nickname == "Admin"));
    }

    /// <summary>An administrator-created nickname is still normalized and still rejected when it hides invisible characters.</summary>
    [Fact]
    public async Task CreateAccountAsync_ReturnsFail_WhenNicknameHasInvisibleCharacters()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAccountAsync(new AdminCreateAccountRequest
        {
            Email = "new@example.com",
            PlainPassword = "Plain1234!",
            Nickname = "Bo\u200Bb"
        }, Actor, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("Account.NicknameInvalidCharacters", result.ErrorCode);
        Assert.Empty(_passwordService.HashPasswordCalls);
    }

    /// <summary>A nickname that differs from an existing one only by case is reported as a conflict, before hashing.</summary>
    [Fact]
    public async Task CreateAccountAsync_ReturnsNicknameConflict_WhenNicknameExistsIgnoringCase()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));
        _accountRepo.AnswerNicknameExistsIgnoreCaseAsync((nicknameArg, _) => nicknameArg == "bob", (_, _) => Task.FromResult<bool>(true));
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAccountAsync(new AdminCreateAccountRequest
        {
            Email = "new@example.com",
            PlainPassword = "Plain1234!",
            Nickname = " bob "
        }, Actor, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal("Account.NicknameAlreadyExists", result.ErrorCode);
        Assert.Contains("bob", result.ErrorArgs);
        Assert.Empty(_passwordService.HashPasswordCalls);
    }

    /// <summary>
    /// Regression test: two concurrent admin creates for the same not-yet-registered nickname can
    /// both pass the prior <c>FindByEmailAsync</c> check (it only checks email) and race on
    /// <c>CreateAsync</c>. The nickname-unique-constraint violation must be classified as a
    /// nickname conflict, not fall through to the generic "Input is invalid" failure.
    /// </summary>
    [Fact]
    public async Task CreateAccountAsync_ReturnsNicknameConflict_WhenNicknameUniqueViolationRaces()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));
        _passwordService.AnswerHashPassword(_ => "$2a$13$hashed");
        // Simulate a unique constraint violation (Npgsql error code 23505) on the Nickname constraint specifically.
        _accountRepo.AnswerCreateAsync((_, _) => Task.FromException(new Exception("duplicate key", new Exception("23505: duplicate key value violates unique constraint \"Account_Nickname_unique\""))));
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAccountAsync(new AdminCreateAccountRequest
        {
            Email = "new@example.com",
            PlainPassword = "Plain1234!",
            Nickname = "TakenNick"
        }, Actor, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal("Account.NicknameAlreadyExists", result.ErrorCode);
        Assert.Contains("Nickname", result.ErrorKey, StringComparison.Ordinal);
        Assert.Contains("TakenNick", result.ErrorArgs);
    }

    /// <summary>
    /// The race between "bob" and "Bob" is caught by the case-insensitive unique index
    /// (<c>Account_unique_index_0</c>) and must be reported as a nickname conflict too.
    /// </summary>
    [Fact]
    public async Task CreateAccountAsync_ReturnsNicknameConflict_WhenCaseInsensitiveIndexRaces()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));
        _passwordService.AnswerHashPassword(_ => "$2a$13$hashed");
        _accountRepo.AnswerCreateAsync((_, _) => Task.FromException(new Exception("duplicate key", new Exception("23505: duplicate key value violates unique constraint \"Account_unique_index_0\""))));
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAccountAsync(new AdminCreateAccountRequest
        {
            Email = "new@example.com",
            PlainPassword = "Plain1234!",
            Nickname = "bob"
        }, Actor, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal("Account.NicknameAlreadyExists", result.ErrorCode);
        Assert.Contains("bob", result.ErrorArgs);
    }

    /// <summary>
    /// Regression test: the same race on the Email primary key (<c>Account_pk</c>) must be reported
    /// as an email conflict, not incorrectly blamed on the nickname.
    /// </summary>
    [Fact]
    public async Task CreateAccountAsync_ReturnsEmailAlreadyExists_WhenEmailPrimaryKeyViolationRaces()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));
        _passwordService.AnswerHashPassword(_ => "$2a$13$hashed");
        // Simulate a unique constraint violation on the Email primary key, not the Nickname constraint.
        _accountRepo.AnswerCreateAsync((_, _) => Task.FromException(new Exception("duplicate key", new Exception("23505: duplicate key value violates unique constraint \"Account_pk\""))));
        var sut = CreateSut();

        ServiceResult result = await sut.CreateAccountAsync(new AdminCreateAccountRequest
        {
            Email = "raced@example.com",
            PlainPassword = "Plain1234!",
            Nickname = "SomeNick"
        }, Actor, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal("Account.AlreadyExists", result.ErrorCode);
        Assert.DoesNotContain("Nickname", result.ErrorKey, StringComparison.Ordinal);
    }

    // -- UpdateAccountAsync ---------------------------------------------------

    /// <summary>Verifies that <c>UpdateAccountAsync</c> returns fail when account not found.</summary>
    [Fact]
    public async Task UpdateAccountAsync_ReturnsFail_WhenAccountNotFound()
    {
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(null));
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateAccountAsync(
            new AdminUpdateAccountRequest { Email = "ghost@example.com" }, null, Actor, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("wrong", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>An administrator sets the account's File Manager quota; a request without one keeps the current quota.</summary>
    [Fact]
    public async Task UpdateAccountAsync_SetsTheFileManagerQuota_OnlyWhenGiven()
    {
        var account = ActiveAccount();
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(account));
        _accountRepo.AnswerUpdateEntityAsync((_, _) => Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult set = await sut.UpdateAccountAsync(
            new AdminUpdateAccountRequest { Email = account.Email.Value, Role = "User", FileManagerQuotaBytes = 5_000 }, null, Actor, TestContext.Current.CancellationToken);
        Assert.True(set.Success);
        Assert.Equal(5_000, account.FileManagerQuotaBytes);

        ServiceResult kept = await sut.UpdateAccountAsync(
            new AdminUpdateAccountRequest { Email = account.Email.Value, Role = "User", FileManagerQuotaBytes = null }, null, Actor, TestContext.Current.CancellationToken);
        Assert.True(kept.Success);
        Assert.Equal(5_000, account.FileManagerQuotaBytes);
    }

    /// <summary>
    /// An administrator allows (or disallows) the account to keep File Manager files that could not be scanned; a request without
    /// the flag keeps the current setting.
    /// </summary>
    [Fact]
    public async Task UpdateAccountAsync_SetsTheFileManagerScanDisabledFlag_OnlyWhenGiven()
    {
        var account = ActiveAccount();
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(account));
        _accountRepo.AnswerUpdateEntityAsync((_, _) => Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult set = await sut.UpdateAccountAsync(
            new AdminUpdateAccountRequest { Email = account.Email.Value, Role = "User", FileManagerScanDisabled = true }, null, Actor, TestContext.Current.CancellationToken);
        Assert.True(set.Success);
        Assert.True(account.FileManagerScanDisabled);

        ServiceResult kept = await sut.UpdateAccountAsync(
            new AdminUpdateAccountRequest { Email = account.Email.Value, Role = "User", FileManagerScanDisabled = null }, null, Actor, TestContext.Current.CancellationToken);
        Assert.True(kept.Success);
        Assert.True(account.FileManagerScanDisabled);

        ServiceResult cleared = await sut.UpdateAccountAsync(
            new AdminUpdateAccountRequest { Email = account.Email.Value, Role = "User", FileManagerScanDisabled = false }, null, Actor, TestContext.Current.CancellationToken);
        Assert.True(cleared.Success);
        Assert.False(account.FileManagerScanDisabled);
    }

    /// <summary>A negative quota is refused with the domain's reason, rolled back and not saved.</summary>
    [Fact]
    public async Task UpdateAccountAsync_RefusesANegativeFileManagerQuota()
    {
        var account = ActiveAccount();
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(account));

        ServiceResult result = await CreateSut().UpdateAccountAsync(
            new AdminUpdateAccountRequest { Email = account.Email.Value, Role = "User", FileManagerQuotaBytes = -1 }, null, Actor, TestContext.Current.CancellationToken);

        Assert.Equal("Account.FileManagerQuotaNegative", result.ErrorCode);
        Assert.Equal("The storage quota cannot be negative.", result.ErrorKey);
        Assert.Empty(_accountRepo.UpdateEntityAsyncCalls);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>Verifies that <c>UpdateAccountAsync</c> keeps existing password when new password is empty.</summary>
    [Fact]
    public async Task UpdateAccountAsync_KeepsExistingPassword_WhenNewPasswordIsEmpty()
    {
        var account = ActiveAccount();
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(account));
        _accountRepo.AnswerUpdateEntityAsync((_, _) => Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateAccountAsync(
            new AdminUpdateAccountRequest { Email = account.Email.Value, Role = "User" }, null, Actor, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Single(_accountRepo.UpdateEntityAsyncCalls, call => (call.Domain!.HashedPassword == account.HashedPassword));
        Assert.Empty(_passwordService.HashPasswordCalls);
    }

    /// <summary>Verifies that <c>UpdateAccountAsync</c> hashes new password when new password provided.</summary>
    [Fact]
    public async Task UpdateAccountAsync_HashesNewPassword_WhenNewPasswordProvided()
    {
        var account = ActiveAccount();
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(account));
        _passwordService.AnswerHashPassword(passwordArg => passwordArg == "NewPass1!", _ => "$2a$13$newhash");
        _accountRepo.AnswerUpdateEntityAsync((_, _) => Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateAccountAsync(
            new AdminUpdateAccountRequest { Email = account.Email.Value, Role = "User" }, "NewPass1!", Actor, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Single(_accountRepo.UpdateEntityAsyncCalls, call => (call.Domain!.HashedPassword == "$2a$13$newhash"));
    }

    /// <summary>An admin-set password forces the account to choose its own new one at next login.</summary>
    [Fact]
    public async Task UpdateAccountAsync_ForcesPasswordChange_WhenNewPasswordProvided()
    {
        var account = ActiveAccount();
        string originalStamp = account.SecurityStamp;
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(account));
        _passwordService.AnswerHashPassword(password => password == "NewPass1!", _ => "$2a$13$newhash");
        _accountRepo.AnswerUpdateEntityAsync((_, _) => Task.CompletedTask);
        var sut = CreateSut();

        await sut.UpdateAccountAsync(
            new AdminUpdateAccountRequest { Email = account.Email.Value, Role = "User" }, "NewPass1!", Actor, TestContext.Current.CancellationToken);

        Assert.True(account.MustChangePassword);
        Assert.NotEqual(originalStamp, account.SecurityStamp);
    }

    /// <summary>Verifies that <c>UpdateAccountAsync</c> resets login attempt when unlocking.</summary>
    [Fact]
    public async Task UpdateAccountAsync_ResetsLoginAttempt_WhenUnlocking()
    {
        var account = ActiveAccount(locked: true, loginAttempt: 5);
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(account));
        _accountRepo.AnswerUpdateEntityAsync((_, _) => Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateAccountAsync(
            new AdminUpdateAccountRequest { Email = account.Email.Value, Locked = false }, null, Actor, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Single(_accountRepo.UpdateEntityAsyncCalls, call => (call.Domain!.Locked == false && call.Domain!.LoginAttempt == 0));
    }

    /// <summary>A persisted account in the opposite state of every admin toggle: deleted, unconfirmed, terms not accepted.</summary>
    private static Account DeletedUnconfirmedAccount() =>
        Account.Reconstitute("user@example.com", "$2a$13$placeholder", "User", null, Role.User, "UTC", null, false, 0,
            false, false, "reg-token", null, DateTime.UtcNow, DateTime.UtcNow, null, true, "stamp", false);

    /// <summary>Every admin toggle is applied in both directions: role, time zone, confirmation, terms, deletion and message.</summary>
    [Fact]
    public async Task UpdateAccountAsync_AppliesEveryAdminChange()
    {
        Account account = DeletedUnconfirmedAccount();
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(account));
        // Another active administrator remains, so the second call may delete this one after it became an Admin.
        _accountRepo.AnswerFindActiveAdminsForUpdateAsync(_ => Task.FromResult<List<Account>>([AdminAccount(Actor)]));

        ServiceResult result = await CreateSut().UpdateAccountAsync(new AdminUpdateAccountRequest
        {
            Email = "user@example.com", Role = Role.Admin, TimeZoneIanaId = "Asia/Seoul",
            EmailConfirmed = true, AgreedServiceTerms = true, Deleted = false, Message = "note"
        }, null, Actor, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(Role.Admin, account.Role.Value);
        Assert.Equal("Asia/Seoul", account.TimeZone.Value);
        Assert.True(account.EmailConfirmed);
        Assert.True(account.AgreedServiceTerms);
        Assert.False(account.Deleted);
        Assert.Equal("note", account.Message);

        result = await CreateSut().UpdateAccountAsync(new AdminUpdateAccountRequest
        {
            Email = "user@example.com", EmailConfirmed = false, AgreedServiceTerms = false, Deleted = true
        }, null, Actor, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(Role.Admin, account.Role.Value); // a null role keeps the current one
        Assert.Equal("Asia/Seoul", account.TimeZone.Value); // so does a null time zone
        Assert.False(account.EmailConfirmed);
        Assert.False(account.AgreedServiceTerms);
        Assert.True(account.Deleted);
        Assert.Null(account.Message);
    }

    /// <summary>An account that stays locked keeps its failed-login count; the admin path cannot set it.</summary>
    [Fact]
    public async Task UpdateAccountAsync_KeepsTheLoginAttemptCount_WhenTheAccountStaysLocked()
    {
        Account account = ActiveAccount(locked: true, loginAttempt: 4);
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(account));

        await CreateSut().UpdateAccountAsync(
            new AdminUpdateAccountRequest { Email = account.Email.Value, Locked = true, Message = "spam" }, null, Actor, TestContext.Current.CancellationToken);

        Assert.True(account.Locked);
        Assert.Equal(4, account.LoginAttempt);
        Assert.Equal("spam", account.Message);
    }

    /// <summary>An unlocked account the admin locks keeps its count and stores the admin's message.</summary>
    [Fact]
    public async Task UpdateAccountAsync_LocksAnUnlockedAccount()
    {
        Account account = ActiveAccount(locked: false, loginAttempt: 2);
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(account));

        await CreateSut().UpdateAccountAsync(
            new AdminUpdateAccountRequest { Email = account.Email.Value, Locked = true, Message = "spam" }, null, Actor, TestContext.Current.CancellationToken);

        Assert.True(account.Locked);
        Assert.Equal(2, account.LoginAttempt);
        Assert.Equal("spam", account.Message);
    }

    /// <summary>
    /// An administrator locking an unlocked account ends its open sessions (new security stamp); re-saving an
    /// account that is already locked — for example one locked by failed logins — leaves its sessions alone.
    /// </summary>
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task UpdateAccountAsync_EndsTheSessions_OnlyWhenTheAdminLocksAnUnlockedAccount(bool alreadyLocked, bool stampReplaced)
    {
        Account account = ActiveAccount(locked: alreadyLocked);
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(account));

        await CreateSut().UpdateAccountAsync(
            new AdminUpdateAccountRequest { Email = account.Email.Value, Locked = true }, null, Actor, TestContext.Current.CancellationToken);

        Assert.True(account.Locked);
        Assert.Equal(stampReplaced, account.SecurityStamp != "stamp");
    }

    /// <summary>An invalid time zone is returned as the domain's validation error and nothing is written.</summary>
    [Fact]
    public async Task UpdateAccountAsync_ReturnsTheDomainValidationError_ForAnUnknownTimeZone()
    {
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(ActiveAccount()));

        ServiceResult result = await CreateSut().UpdateAccountAsync(new AdminUpdateAccountRequest { Email = "user@example.com", TimeZoneIanaId = "Not/AZone" }, null, Actor, TestContext.Current.CancellationToken);

        Assert.Equal("TimeZoneId.Invalid", result.ErrorCode);
        Assert.Empty(_accountRepo.UpdateEntityAsyncCalls);
    }

    // -- DeleteAccountAsync ---------------------------------------------------

    /// <summary>Verifies that <c>DeleteAccountAsync</c> returns fail when account not found.</summary>
    [Fact]
    public async Task DeleteAccountAsync_ReturnsFail_WhenAccountNotFound()
    {
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(null));
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAccountAsync("ghost@example.com", Actor, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("find", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>DeleteAccountAsync</c> sets deleted flag when account found.</summary>
    [Fact]
    public async Task DeleteAccountAsync_SetsDeletedFlag_WhenAccountFound()
    {
        var account = ActiveAccount();
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(account));
        _accountRepo.AnswerUpdateEntityAsync((_, _) => Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteAccountAsync(account.Email.Value, Actor, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Single(_accountRepo.UpdateEntityAsyncCalls, call => (call.Domain!.Deleted == true));
    }

    // -- SearchAccountsAsync ------------------------------------------------------

    /// <summary>The admin grid search is delegated to the repository and its rows carry no password hash and no token.</summary>
    [Fact]
    public async Task SearchAccountsAsync_DelegatesToRepository_AndMapsTheRows()
    {
        _accountRepo.AnswerSearchAsync((search, _) => search == "needle", (_, _) => Task.FromResult<List<Account>>([ActiveAccount("found@example.com")]));

        var row = Assert.Single(await CreateSut().SearchAccountsAsync("needle", TestContext.Current.CancellationToken));

        Assert.Equal("found@example.com", row.Email);
        Assert.All(SecretColumns, name => Assert.Null(row.GetType().GetProperty(name)));
    }

    // -- CreateAccountAsync: validation and failures ---------------------------------

    /// <summary>An admin create-account request with the given e-mail, password and nickname.</summary>
    private static AdminCreateAccountRequest NewAccountRequest(string email = "new@example.com", string password = "Plain1234!", string nickname = "NewUser") => new()
    {
        Email = email, PlainPassword = password, Nickname = nickname, EmailConfirmed = true
    };

    /// <summary>An admin-created account still has to meet the password rule; nothing is hashed or stored otherwise.</summary>
    [Fact]
    public async Task CreateAccountAsync_ReturnsPolicyViolation_AndDoesNotHashOrCreate_WhenThePasswordIsTooWeak()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));

        ServiceResult result = await CreateSut().CreateAccountAsync(NewAccountRequest(password: "weak"), Actor, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("Account.PasswordPolicy", result.ErrorCode);
        Assert.Empty(_passwordService.HashPasswordCalls);
        Assert.Empty(_accountRepo.CreateAsyncCalls);
    }

    /// <summary>An account role other than Admin or User (e.g. the menu-only Anonymous) is refused before anything is hashed or stored.</summary>
    [Fact]
    public async Task CreateAccountAsync_RejectsARoleOtherThanAdminOrUser()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));
        AdminCreateAccountRequest request = NewAccountRequest();
        request.Role = Role.Anonymous;

        ServiceResult result = await CreateSut().CreateAccountAsync(request, Actor, TestContext.Current.CancellationToken);

        Assert.Equal("Account.RoleInvalid", result.ErrorCode);
        Assert.Empty(_passwordService.HashPasswordCalls);
        Assert.Empty(_accountRepo.CreateAsyncCalls);
    }

    /// <summary>A malformed address is returned as the domain's validation error and nothing is stored.</summary>
    [Fact]
    public async Task CreateAccountAsync_ReturnsTheDomainValidationError_ForAMalformedEmail()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));
        _passwordService.AnswerHashPassword(_ => "$2a$13$hashed");

        ServiceResult result = await CreateSut().CreateAccountAsync(NewAccountRequest(email: "not-an-email"), Actor, TestContext.Current.CancellationToken);

        Assert.Equal("Email.Invalid", result.ErrorCode);
        Assert.Empty(_accountRepo.CreateAsyncCalls);
    }

    /// <summary>The admin's "email confirmed" checkbox and status message are applied to the created account.</summary>
    [Fact]
    public async Task CreateAccountAsync_AppliesTheConfirmedFlagAndMessage_FromTheRequest()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));
        _passwordService.AnswerHashPassword(_ => "$2a$13$hashed");
        AdminCreateAccountRequest request = NewAccountRequest();
        request.Message = "created by admin";
        request.EmailConfirmed = true;

        ServiceResult result = await CreateSut().CreateAccountAsync(request, Actor, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Single(_accountRepo.CreateAsyncCalls, call => (call.Domain!.EmailConfirmed && call.Domain!.Message == "created by admin" && call.Domain!.RegistrationToken == null));
    }

    /// <summary>An unexpected repository failure is caught and reported without leaking internals.</summary>
    [Fact]
    public async Task CreateAccountAsync_ReportsCreateFailed_WhenTheRepositoryThrows()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromException<Account?>(new InvalidOperationException("secret detail")));

        ServiceResult result = await CreateSut().CreateAccountAsync(NewAccountRequest(), Actor, TestContext.Current.CancellationToken);

        Assert.Equal("ManagementAccount.CreateFailed", result.ErrorCode);
        Assert.Equal(ServiceResult.TemporaryErrorKey, result.ErrorKey);
        Assert.DoesNotContain("secret detail", result.ErrorKey, StringComparison.Ordinal);
    }

    // -- UpdateAccountAsync: validation and failures ----------------------------------

    /// <summary>An admin-set password must meet the policy: refused before hashing and before the row lock is taken.</summary>
    [Fact]
    public async Task UpdateAccountAsync_ReturnsPolicyViolation_BeforeLockingTheRow_WhenTheNewPasswordIsTooWeak()
    {
        ServiceResult result = await CreateSut().UpdateAccountAsync(new AdminUpdateAccountRequest { Email = "user@example.com" }, "weak", Actor, TestContext.Current.CancellationToken);

        Assert.Equal("Account.PasswordPolicy", result.ErrorCode);
        Assert.Empty(_passwordService.HashPasswordCalls);
        Assert.DoesNotContain(_unitOfWork.BeginAsyncCalls, call => call.IsolationLevel == null);
    }

    /// <summary>An unknown role is returned as the domain's validation error; the row is not written and the lock is released.</summary>
    [Fact]
    public async Task UpdateAccountAsync_ReturnsTheDomainValidationError_ForAnUnknownRole()
    {
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(ActiveAccount()));

        ServiceResult result = await CreateSut().UpdateAccountAsync(new AdminUpdateAccountRequest { Email = "user@example.com", Role = "SuperUser", TimeZoneIanaId = "UTC" }, null, Actor, TestContext.Current.CancellationToken);

        Assert.Equal("Account.RoleInvalid", result.ErrorCode);
        Assert.Empty(_accountRepo.UpdateEntityAsyncCalls);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>An unexpected repository failure on update is rolled back and reported without leaking internals.</summary>
    [Fact]
    public async Task UpdateAccountAsync_RollsBackAndReportsUpdateFailed_WhenTheRepositoryThrows()
    {
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromException<Account?>(new InvalidOperationException("secret detail")));

        ServiceResult result = await CreateSut().UpdateAccountAsync(new AdminUpdateAccountRequest { Email = "user@example.com" }, null, Actor, TestContext.Current.CancellationToken);

        Assert.Equal("ManagementAccount.UpdateFailed", result.ErrorCode);
        Assert.Equal(ServiceResult.TemporaryErrorKey, result.ErrorKey);
        Assert.DoesNotContain("secret detail", result.ErrorKey, StringComparison.Ordinal);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
        Assert.Empty(_unitOfWork.CommitAsyncCalls);
    }

    /// <summary>An unexpected repository failure on delete is rolled back and reported without leaking internals.</summary>
    [Fact]
    public async Task DeleteAccountAsync_RollsBackAndReportsDeleteFailed_WhenTheRepositoryThrows()
    {
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromException<Account?>(new InvalidOperationException("secret detail")));

        ServiceResult result = await CreateSut().DeleteAccountAsync("user@example.com", Actor, TestContext.Current.CancellationToken);

        Assert.Equal("ManagementAccount.DeleteFailed", result.ErrorCode);
        Assert.Equal(ServiceResult.TemporaryErrorKey, result.ErrorKey);
        Assert.DoesNotContain("secret detail", result.ErrorKey, StringComparison.Ordinal);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    // -- Exact results, lookup keys, defaults, transaction boundaries and failure logging ---

    /// <summary>Asserts the one Error entry carries <paramref name="thrown"/> and reads <paramref name="message"/>.</summary>
    private void AssertLoggedError(string message, Exception thrown)
    {
        FakeLogRecord record = Assert.Single(_logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Error);
        Assert.Same(thrown, record.Exception);
        Assert.Equal(message, record.Message);
    }

    /// <summary>The duplicate-email check looks up the requested e-mail; an existing one is the "already created" Conflict.</summary>
    [Fact]
    public async Task CreateAccountAsync_ReturnsAlreadyExists_WhenTheRequestedEmailIsTaken()
    {
        _accountRepo.AnswerFindByEmailAsync((email, _) => email == "taken@example.com", (_, _) => Task.FromResult<Account?>(ActiveAccount("taken@example.com")));

        ServiceResult result = await CreateSut().CreateAccountAsync(NewAccountRequest("taken@example.com"), Actor, TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal("Account.AlreadyExists", result.ErrorCode);
        Assert.Equal("This account has already been created.", result.ErrorKey);
        Assert.Empty(_accountRepo.CreateAsyncCalls);
    }

    /// <summary>A nickname taken (ignoring case) is a Conflict whose message template takes the nickname as its argument.</summary>
    [Fact]
    public async Task CreateAccountAsync_ReturnsTheNicknameConflictTemplate_WhenTheNicknameIsTaken()
    {
        _accountRepo.AnswerNicknameExistsIgnoreCaseAsync((nickname, _) => nickname == "NewUser", (_, _) => Task.FromResult<bool>(true));

        ServiceResult result = await CreateSut().CreateAccountAsync(NewAccountRequest(), Actor, TestContext.Current.CancellationToken);

        Assert.Equal("'{0}' is a Nickname that already exists. Please enter another Nickname.", result.ErrorKey);
        Assert.Equal(["NewUser"], result.ErrorArgs);
    }

    /// <summary>A concurrent create that hits the e-mail primary key is the "already created" Conflict, with its message.</summary>
    [Fact]
    public async Task CreateAccountAsync_ReturnsTheAlreadyExistsMessage_WhenTheEmailPrimaryKeyRaces()
    {
        _passwordService.AnswerHashPassword(_ => "$2a$13$hashed");
        _accountRepo.AnswerCreateAsync((_, _) => Task.FromException(new Exception("duplicate key", new Exception("23505: duplicate key value violates unique constraint \"Account_pk\""))));

        ServiceResult result = await CreateSut().CreateAccountAsync(NewAccountRequest(), Actor, TestContext.Current.CancellationToken);

        Assert.Equal("This account has already been created.", result.ErrorKey);
    }

    /// <summary>An account created without a time zone gets UTC.</summary>
    [Fact]
    public async Task CreateAccountAsync_DefaultsTheTimeZoneToUtc_WhenNoneIsGiven()
    {
        _passwordService.AnswerHashPassword(_ => "$2a$13$hashed");
        AdminCreateAccountRequest request = NewAccountRequest();
        request.TimeZoneIanaId = null;

        ServiceResult result = await CreateSut().CreateAccountAsync(request, Actor, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Single(_accountRepo.CreateAsyncCalls, call => (call.Domain!.TimeZone.Value == "UTC"));
    }

    /// <summary>The requested time zone is stored on the new account.</summary>
    [Fact]
    public async Task CreateAccountAsync_StoresTheRequestedTimeZone()
    {
        _passwordService.AnswerHashPassword(_ => "$2a$13$hashed");
        AdminCreateAccountRequest request = NewAccountRequest();
        request.TimeZoneIanaId = "Asia/Seoul";

        await CreateSut().CreateAccountAsync(request, Actor, TestContext.Current.CancellationToken);

        Assert.Single(_accountRepo.CreateAsyncCalls, call => (call.Domain!.TimeZone.Value == "Asia/Seoul"));
    }

    /// <summary>A create that throws unexpectedly is logged with the exception and the e-mail.</summary>
    [Fact]
    public async Task CreateAccountAsync_LogsTheFailure_WhenTheRepositoryThrows()
    {
        _passwordService.AnswerHashPassword(_ => "$2a$13$hashed");
        var thrown = new InvalidOperationException("boom");
        _accountRepo.AnswerCreateAsync((_, _) => Task.FromException(thrown));

        await CreateSut().CreateAccountAsync(NewAccountRequest(), Actor, TestContext.Current.CancellationToken);

        AssertLoggedError("Failed to create account for new@example.com", thrown);
    }

    /// <summary>A successful admin update locks the requested account's row inside a transaction and commits it.</summary>
    [Fact]
    public async Task UpdateAccountAsync_LocksTheRequestedAccountAndCommits_OnSuccess()
    {
        Account account = ActiveAccount();
        _accountRepo.AnswerFindByEmailForUpdateAsync((emailArg, _) => emailArg == "user@example.com", (_, _) => Task.FromResult<Account?>(account));

        ServiceResult result = await CreateSut().UpdateAccountAsync(
            new AdminUpdateAccountRequest { Email = "user@example.com" }, null, Actor, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Single(_unitOfWork.BeginAsyncCalls, call => call.IsolationLevel == null);
        Assert.Single(_unitOfWork.CommitAsyncCalls);
        Assert.Empty(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>An unknown account on update is a NotFound ("Email address is wrong"), rolled back.</summary>
    [Fact]
    public async Task UpdateAccountAsync_ReturnsNotFound_AndRollsBack_WhenTheAccountIsUnknown()
    {
        ServiceResult result = await CreateSut().UpdateAccountAsync(
            new AdminUpdateAccountRequest { Email = "ghost@example.com" }, null, Actor, TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("Account.NotFound", result.ErrorCode);
        Assert.Equal("Email address is wrong", result.ErrorKey);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
        Assert.Empty(_unitOfWork.CommitAsyncCalls);
    }

    /// <summary>An update that throws unexpectedly is logged with the exception and the e-mail.</summary>
    [Fact]
    public async Task UpdateAccountAsync_LogsTheFailure_WhenTheRepositoryThrows()
    {
        _accountRepo.AnswerFindByEmailForUpdateAsync((emailArg, _) => emailArg == "user@example.com", (_, _) => Task.FromResult<Account?>(ActiveAccount()));
        var thrown = new InvalidOperationException("boom");
        _accountRepo.AnswerUpdateEntityAsync((_, _) => Task.FromException(thrown));

        await CreateSut().UpdateAccountAsync(new AdminUpdateAccountRequest { Email = "user@example.com" }, null, Actor, TestContext.Current.CancellationToken);

        AssertLoggedError("Failed to update account for user@example.com", thrown);
    }

    /// <summary>A successful admin delete runs inside a transaction and commits it.</summary>
    [Fact]
    public async Task DeleteAccountAsync_BeginsAndCommitsTheTransaction_OnSuccess()
    {
        _accountRepo.AnswerFindByEmailForUpdateAsync((emailArg, _) => emailArg == "user@example.com", (_, _) => Task.FromResult<Account?>(ActiveAccount()));

        ServiceResult result = await CreateSut().DeleteAccountAsync("user@example.com", Actor, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Single(_unitOfWork.BeginAsyncCalls, call => call.IsolationLevel == null);
        Assert.Single(_unitOfWork.CommitAsyncCalls);
        Assert.Empty(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>An unknown account on delete is a NotFound with its message, rolled back.</summary>
    [Fact]
    public async Task DeleteAccountAsync_ReturnsNotFound_AndRollsBack_WhenTheAccountIsUnknown()
    {
        ServiceResult result = await CreateSut().DeleteAccountAsync("ghost@example.com", Actor, TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("Account.NotFound", result.ErrorCode);
        Assert.Equal("Fail to find the account by given email address", result.ErrorKey);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
        Assert.Empty(_unitOfWork.CommitAsyncCalls);
    }

    /// <summary>A delete that throws unexpectedly is logged with the exception and the e-mail.</summary>
    [Fact]
    public async Task DeleteAccountAsync_LogsTheFailure_WhenTheRepositoryThrows()
    {
        _accountRepo.AnswerFindByEmailForUpdateAsync((emailArg, _) => emailArg == "user@example.com", (_, _) => Task.FromResult<Account?>(ActiveAccount()));
        var thrown = new InvalidOperationException("boom");
        _accountRepo.AnswerUpdateEntityAsync((_, _) => Task.FromException(thrown));

        await CreateSut().DeleteAccountAsync("user@example.com", Actor, TestContext.Current.CancellationToken);

        AssertLoggedError("Failed to delete account for user@example.com", thrown);
    }

    // -- Admin safeguards (AdminSafeguardPolicy) ------------------------------

    /// <summary>A persisted, active administrator account.</summary>
    private static Account AdminAccount(string email, bool locked = false) =>
        Account.Reconstitute(email, "$2a$13$placeholder", "Admin-" + email, null, Role.Admin, "UTC", null, locked, 0,
            true, true, null, null, DateTime.UtcNow, DateTime.UtcNow, null, false, "stamp", false);

    /// <summary>Returns <paramref name="target"/> as the locked account and <paramref name="activeAdmins"/> as the locked active administrators.</summary>
    private void SetUpAccounts(Account target, params Account[] activeAdmins)
    {
        _accountRepo.AnswerFindByEmailForUpdateAsync((emailArg, _) => emailArg == target.Email.Value, (_, _) => Task.FromResult<Account?>(target));
        _accountRepo.AnswerFindActiveAdminsForUpdateAsync(_ => Task.FromResult<List<Account>>([.. activeAdmins]));
    }

    /// <summary>Asserts a refusal: the exact error, rolled back, nothing written, and a Warning naming account, admin and code.</summary>
    private void AssertRefused(ServiceResult result, ServiceErrorType type, string code, string key, string loggedMessage)
    {
        Assert.False(result.Success);
        Assert.Equal(type, result.ErrorType);
        Assert.Equal(code, result.ErrorCode);
        Assert.Equal(key, result.ErrorKey);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
        Assert.Empty(_unitOfWork.CommitAsyncCalls);
        Assert.Empty(_accountRepo.UpdateEntityAsyncCalls);
        FakeLogRecord record = Assert.Single(_logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Warning);
        Assert.Equal(loggedMessage, record.Message);
        Assert.Null(record.Exception);
    }

    /// <summary>An administrator cannot lock their own account, even while other administrators remain.</summary>
    [Fact]
    public async Task UpdateAccountAsync_RefusesLockingYourOwnAccount()
    {
        Account self = AdminAccount(Actor);
        SetUpAccounts(self, self, AdminAccount("other@example.com"));

        ServiceResult result = await CreateSut().UpdateAccountAsync(
            new AdminUpdateAccountRequest { Email = Actor, Locked = true }, null, Actor, TestContext.Current.CancellationToken);

        AssertRefused(result, ServiceErrorType.Validation, "Account.CannotLockSelf", "You cannot lock your own account.",
            $"Admin update refused for {Actor} by admin {Actor}: Account.CannotLockSelf");
    }

    /// <summary>An administrator cannot take the Admin role off their own account.</summary>
    [Fact]
    public async Task UpdateAccountAsync_RefusesDemotingYourOwnAccount()
    {
        Account self = AdminAccount(Actor);
        SetUpAccounts(self, self, AdminAccount("other@example.com"));

        ServiceResult result = await CreateSut().UpdateAccountAsync(
            new AdminUpdateAccountRequest { Email = Actor, Role = Role.User }, null, Actor, TestContext.Current.CancellationToken);

        AssertRefused(result, ServiceErrorType.Validation, "Account.CannotDemoteSelf", "You cannot remove the Admin role from your own account.",
            $"Admin update refused for {Actor} by admin {Actor}: Account.CannotDemoteSelf");
    }

    /// <summary>An administrator cannot delete their own account from the edit form either.</summary>
    [Fact]
    public async Task UpdateAccountAsync_RefusesDeletingYourOwnAccount()
    {
        Account self = AdminAccount(Actor);
        SetUpAccounts(self, self, AdminAccount("other@example.com"));

        ServiceResult result = await CreateSut().UpdateAccountAsync(
            new AdminUpdateAccountRequest { Email = Actor, Deleted = true }, null, Actor, TestContext.Current.CancellationToken);

        AssertRefused(result, ServiceErrorType.Validation, "Account.CannotDeleteSelf", "You cannot delete your own account.",
            $"Admin update refused for {Actor} by admin {Actor}: Account.CannotDeleteSelf");
    }

    /// <summary>Editing your own account without locking, deleting or demoting it still works.</summary>
    [Fact]
    public async Task UpdateAccountAsync_AllowsAnOrdinaryEditOfYourOwnAccount()
    {
        Account self = AdminAccount(Actor);
        SetUpAccounts(self, self);

        ServiceResult result = await CreateSut().UpdateAccountAsync(
            new AdminUpdateAccountRequest { Email = Actor, Message = "note" }, null, Actor, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal("note", self.Message);
        Assert.Single(_unitOfWork.CommitAsyncCalls);
    }

    /// <summary>Nobody can lock the last active administrator.</summary>
    [Fact]
    public async Task UpdateAccountAsync_RefusesLockingTheLastActiveAdmin()
    {
        Account target = AdminAccount("last@example.com");
        SetUpAccounts(target, target);

        ServiceResult result = await CreateSut().UpdateAccountAsync(
            new AdminUpdateAccountRequest { Email = "last@example.com", Locked = true }, null, Actor, TestContext.Current.CancellationToken);

        AssertRefused(result, ServiceErrorType.Conflict, "Account.LastActiveAdmin", "The last active administrator cannot be locked, deleted or demoted.",
            $"Admin update refused for last@example.com by admin {Actor}: Account.LastActiveAdmin");
    }

    /// <summary>Another administrator can be locked while a different active administrator remains.</summary>
    [Fact]
    public async Task UpdateAccountAsync_AllowsLockingAnotherAdmin_WhileAnotherActiveAdminRemains()
    {
        Account target = AdminAccount("other@example.com");
        SetUpAccounts(target, AdminAccount(Actor), target);

        ServiceResult result = await CreateSut().UpdateAccountAsync(
            new AdminUpdateAccountRequest { Email = "other@example.com", Locked = true }, null, Actor, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.True(target.Locked);
        Assert.Single(_unitOfWork.CommitAsyncCalls);
    }

    /// <summary>
    /// The active administrators are locked before the edited account, so every admin edit takes its row locks
    /// in the same order and two concurrent edits cannot each count the other administrator as still active.
    /// </summary>
    [Fact]
    public async Task UpdateAccountAsync_LocksTheActiveAdminsBeforeTheEditedAccount()
    {
        List<string> calls = [];
        _accountRepo.AnswerFindActiveAdminsForUpdateAsync(_ => { calls.Add("admins"); return Task.FromResult<List<Account>>([AdminAccount(Actor)]); });
        _accountRepo.AnswerFindByEmailForUpdateAsync((email, _) => email == "user@example.com", (_, _) => { calls.Add("account"); return Task.FromResult<Account?>(ActiveAccount()); });

        await CreateSut().UpdateAccountAsync(new AdminUpdateAccountRequest { Email = "user@example.com" }, null, Actor, TestContext.Current.CancellationToken);

        Assert.Equal(["admins", "account"], calls);
    }

    /// <summary>An administrator cannot delete their own account; the comparison ignores the e-mail's case.</summary>
    [Fact]
    public async Task DeleteAccountAsync_RefusesDeletingYourOwnAccount_WhateverTheCase()
    {
        Account self = AdminAccount(Actor);
        SetUpAccounts(self, self, AdminAccount("other@example.com"));

        ServiceResult result = await CreateSut().DeleteAccountAsync(Actor, Actor.ToUpperInvariant(), TestContext.Current.CancellationToken);

        AssertRefused(result, ServiceErrorType.Validation, "Account.CannotDeleteSelf", "You cannot delete your own account.",
            $"Admin delete refused for {Actor} by admin {Actor.ToUpperInvariant()}: Account.CannotDeleteSelf");
    }

    /// <summary>Nobody can delete the last active administrator.</summary>
    [Fact]
    public async Task DeleteAccountAsync_RefusesDeletingTheLastActiveAdmin()
    {
        Account target = AdminAccount("last@example.com");
        SetUpAccounts(target, target);

        ServiceResult result = await CreateSut().DeleteAccountAsync("last@example.com", Actor, TestContext.Current.CancellationToken);

        AssertRefused(result, ServiceErrorType.Conflict, "Account.LastActiveAdmin", "The last active administrator cannot be locked, deleted or demoted.",
            $"Admin delete refused for last@example.com by admin {Actor}: Account.LastActiveAdmin");
    }

    /// <summary>Another administrator can be deleted while a different active administrator remains.</summary>
    [Fact]
    public async Task DeleteAccountAsync_AllowsDeletingAnotherAdmin_WhileAnotherActiveAdminRemains()
    {
        Account target = AdminAccount("other@example.com");
        SetUpAccounts(target, AdminAccount(Actor), target);

        ServiceResult result = await CreateSut().DeleteAccountAsync("other@example.com", Actor, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.True(target.Deleted);
        Assert.Single(_unitOfWork.CommitAsyncCalls);
    }

    /// <summary>A delete locks the active administrators before the deleted account (same order as an update).</summary>
    [Fact]
    public async Task DeleteAccountAsync_LocksTheActiveAdminsBeforeTheDeletedAccount()
    {
        List<string> calls = [];
        _accountRepo.AnswerFindActiveAdminsForUpdateAsync(_ => { calls.Add("admins"); return Task.FromResult<List<Account>>([AdminAccount(Actor)]); });
        _accountRepo.AnswerFindByEmailForUpdateAsync((email, _) => email == "user@example.com", (_, _) => { calls.Add("account"); return Task.FromResult<Account?>(ActiveAccount()); });

        await CreateSut().DeleteAccountAsync("user@example.com", Actor, TestContext.Current.CancellationToken);

        Assert.Equal(["admins", "account"], calls);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _unitOfWork.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
