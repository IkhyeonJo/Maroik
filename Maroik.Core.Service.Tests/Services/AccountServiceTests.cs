using System.Diagnostics;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Enums;
using Maroik.Core.Contract.Extensions;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Settings;
using Maroik.Core.Domain.Accounts;
using Maroik.Core.Service.Services;
using Maroik.Core.Service.Tests.Fixtures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
// ReSharper disable AccessToDisposedClosure

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// Unit tests for <see cref="AccountService"/>.
/// All external dependencies (repository, password service, mail client, email publisher, data protection service)
/// are replaced with hand-written fakes so tests run without a database, SMTP server, or RabbitMQ broker.
/// Covers login, registration, email confirmation, and password-reset flows.
/// </summary>
public class AccountServiceTests : IAsyncDisposable
{
    /// <summary>Fake <c>IAccountRepository</c> injected into the system under test.</summary>
    private readonly FakeAccountRepository _accountRepo = new();
    /// <summary>Fake <c>IPasswordService</c> injected into the system under test.</summary>
    private readonly FakePasswordService _passwordService = new();
    /// <summary>Fake <c>IMailClient</c> injected into the system under test.</summary>
    private readonly FakeMailClient _mailClient = new();
    /// <summary>Fake <c>IEmailPublisher</c> injected into the system under test.</summary>
    private readonly FakeEmailPublisher _emailPublisher = new();
    /// <summary>Fake <c>IDataProtectionService</c> injected into the system under test.</summary>
    private readonly FakeDataProtectionService _dataProtection = new();
    /// <summary>Fake <c>IUnitOfWork</c> injected into the system under test.</summary>
    private readonly FakeUnitOfWork _unitOfWork = new();
    /// <summary>Captures the log entries the system under test writes.</summary>
    private readonly FakeLogger<AccountService> _logger = new();
    /// <summary>Settings with a 5-attempt lockout threshold.</summary>
    private readonly IOptions<ServerSetting> _settings =
        Options.Create(new ServerSetting { MaxLoginAttempt = 5, DomainName = "https://example.com" });

    /// <summary>Minimal mail template passed to the mail-sending methods.</summary>
    private static readonly EmailTemplate _emailTemplate = new() { Subject = "subject", Title = "title", Content0 = "c0", Content1 = "c1" };

    /// <summary>
    /// The password the confirmation tests present with the mailed link. Every test accepts it as
    /// the registration password unless it sets its own <see cref="IPasswordService.VerifyPassword"/>
    /// expectation; login tests use other passwords, so this match never leaks into them.
    /// </summary>
    private const string ConfirmPassword = "Confirm-Pass1!";

    /// <summary>Accepts <see cref="ConfirmPassword"/> as the registration password of every fake account.</summary>
    public AccountServiceTests() =>
        _passwordService.AnswerVerifyPassword((passwordArg, _) => passwordArg == ConfirmPassword, (_, _) => true);

    /// <summary>The fixed "current time" of these tests.</summary>
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
    /// <summary>The clock the service under test reads, stopped at <see cref="Now"/>.</summary>
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(Now));

    /// <summary>The service under test over the fake dependencies.</summary>
    private AccountService CreateSut() => new(
        _accountRepo,
        _passwordService,
        _mailClient,
        _emailPublisher,
        _settings,
        _dataProtection,
        _logger,
        _unitOfWork,
        _time);

    // -- Helpers --------------------------------------------------------------

    /// <summary>A persisted account (confirmed, unlocked, terms agreed unless overridden) whose state is set by the arguments.</summary>
    private static Account ActiveAccount(
        string email = "user@example.com",
        bool deleted = false,
        bool locked = false,
        bool emailConfirmed = true,
        bool agreedServiceTerms = true,
        long loginAttempt = 0,
        string? registrationToken = null,
        string? resetPasswordToken = null) =>
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
            emailConfirmed: emailConfirmed,
            agreedServiceTerms: agreedServiceTerms,
            registrationToken: registrationToken,
            resetPasswordToken: resetPasswordToken,
            created: DateTime.UtcNow,
            updated: DateTime.UtcNow,
            message: null,
            deleted: deleted,
            securityStamp: "stamp",
            mustChangePassword: false);

    /// <summary>Arranges mail bodies to render and the e-mail queue publish to succeed.</summary>
    private void SetupMailSuccess()
    {
        _mailClient.AnswerGetMailConfirmationBody((_, _, _, _, _) => "email-body");
        _mailClient.AnswerGetMailResetPasswordBody((_, _, _, _, _) => "email-body");
        _emailPublisher.AnswerPublishAsync((_, _) => Task.CompletedTask);
    }

    /// <summary>Arranges mail bodies to render but the e-mail queue publish to throw (broker unreachable).</summary>
    private void SetupMailFailure()
    {
        _mailClient.AnswerGetMailConfirmationBody((_, _, _, _, _) => "email-body");
        _mailClient.AnswerGetMailResetPasswordBody((_, _, _, _, _) => "email-body");
        _emailPublisher.AnswerPublishAsync((_, _) => Task.FromException(new InvalidOperationException("RabbitMQ unreachable")));
    }

    // -- LoginAsync -----------------------------------------------------------

    /// <summary>Verifies that <c>LoginAsync</c> returns fail when account not found.</summary>
    [Fact]
    public async Task LoginAsync_ReturnsFail_WhenAccountNotFound()
    {
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(null));
        var sut = CreateSut();

        LoginResult result = await sut.LoginAsync("missing@example.com", "any", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("wrong", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>LoginAsync</c> returns fail when account is deleted.</summary>
    [Fact]
    public async Task LoginAsync_ReturnsFail_WhenAccountIsDeleted()
    {
        var account = ActiveAccount(deleted: true);
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(account));
        // The account-state message is only disclosed once the password proves account ownership.
        _passwordService.AnswerVerifyPassword((_, _) => true);
        var sut = CreateSut();

        LoginResult result = await sut.LoginAsync(account.Email.Value, "correct", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("Deleted", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>LoginAsync</c> does not disclose a deleted account to a wrong password.</summary>
    [Fact]
    public async Task LoginAsync_ReturnsGenericError_WhenAccountIsDeleted_AndPasswordIsWrong()
    {
        var account = ActiveAccount(deleted: true);
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(account));
        _passwordService.AnswerVerifyPassword((_, _) => false);
        _accountRepo.AnswerUpdateEntityAsync((_, _) => Task.CompletedTask);
        var sut = CreateSut();

        LoginResult result = await sut.LoginAsync(account.Email.Value, "wrong", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("wrong", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Deleted", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// A locked account is refused as "locked" whatever password is supplied — right or wrong — and
    /// the password is never even verified, so the lock really ends the guessing and a correct
    /// guess is indistinguishable from a wrong one.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task LoginAsync_ReturnsLocked_WithoutVerifyingThePassword_WhenAccountIsLocked(bool passwordIsCorrect)
    {
        var account = ActiveAccount(locked: true, loginAttempt: 3);
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(account));
        _passwordService.AnswerVerifyPassword((_, _) => passwordIsCorrect);
        var sut = CreateSut();

        LoginResult result = await sut.LoginAsync(account.Email.Value, "whatever", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("Locked", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(_passwordService.VerifyPasswordCalls);
        // Refused up front: no failed-attempt write and no commit of a mutated row.
        Assert.Empty(_accountRepo.UpdateEntityAsyncCalls);
        Assert.Empty(_unitOfWork.CommitAsyncCalls);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>Verifies that <c>LoginAsync</c> returns a failure result and persists the incremented counter when the password is wrong.</summary>
    [Fact]
    public async Task LoginAsync_ReturnsFail_AndPersistsIncrementedCounter_WhenPasswordIsWrong()
    {
        var account = ActiveAccount(loginAttempt: 1);
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(account));
        _passwordService.AnswerVerifyPassword((_, _) => false);
        _accountRepo.AnswerUpdateEntityAsync((_, _) => Task.CompletedTask);
        var sut = CreateSut();

        LoginResult result = await sut.LoginAsync(account.Email.Value, "wrong", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("wrong", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
        // The row-locked account is mutated in memory and persisted within the same transaction,
        // not via a separate atomic UPDATE.
        Assert.Single(_accountRepo.UpdateEntityAsyncCalls, call => (call.Domain!.LoginAttempt == 2 && !call.Domain!.Locked));
        Assert.Single(_unitOfWork.CommitAsyncCalls);
    }

    /// <summary>Verifies that <c>LoginAsync</c> locks the account when the failed attempt reaches max login attempts.</summary>
    [Fact]
    public async Task LoginAsync_LocksAccount_WhenMaxLoginAttemptsReached()
    {
        var account = ActiveAccount(loginAttempt: _settings.Value.MaxLoginAttempt - 1);
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(account));
        _passwordService.AnswerVerifyPassword((_, _) => false);
        _accountRepo.AnswerUpdateEntityAsync((_, _) => Task.CompletedTask);
        var sut = CreateSut();

        LoginResult result = await sut.LoginAsync(account.Email.Value, "wrong", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        // The wrong-password response is always the generic error — it must not reveal that this
        // failed attempt just locked the account (that would enumerate valid emails). The account
        // is still locked in persisted state; the user learns that on their next attempt.
        Assert.Contains("wrong", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
        Assert.Single(_accountRepo.UpdateEntityAsyncCalls, call => (call.Domain!.Locked && call.Domain!.LoginAttempt == _settings.Value.MaxLoginAttempt));
    }

    /// <summary>Verifies that <c>LoginAsync</c> returns fail when email not confirmed.</summary>
    [Fact]
    public async Task LoginAsync_ReturnsFail_WhenEmailNotConfirmed()
    {
        var account = ActiveAccount(emailConfirmed: false);
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(account));
        _passwordService.AnswerVerifyPassword((_, _) => true);
        _accountRepo.AnswerUpdateEntityAsync((_, _) => Task.CompletedTask);
        var sut = CreateSut();

        LoginResult result = await sut.LoginAsync(account.Email.Value, "correct", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("Email verification", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>LoginAsync</c> returns fail when service terms not agreed.</summary>
    [Fact]
    public async Task LoginAsync_ReturnsFail_WhenServiceTermsNotAgreed()
    {
        var account = ActiveAccount(agreedServiceTerms: false);
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(account));
        _passwordService.AnswerVerifyPassword((_, _) => true);
        _accountRepo.AnswerUpdateEntityAsync((_, _) => Task.CompletedTask);
        var sut = CreateSut();

        LoginResult result = await sut.LoginAsync(account.Email.Value, "correct", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("Service Terms", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>LoginAsync</c> returns a success result with account data when credentials are valid.</summary>
    [Fact]
    public async Task LoginAsync_ReturnsSuccessWithAccount_WhenCredentialsValid()
    {
        var account = ActiveAccount();
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(account));
        _passwordService.AnswerVerifyPassword((_, _) => true);
        _accountRepo.AnswerUpdateEntityAsync((_, _) => Task.CompletedTask);
        var sut = CreateSut();

        LoginResult result = await sut.LoginAsync(account.Email.Value, "correct", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.NotNull(result.Account);
        Assert.Equal(account.Email.Value, result.Account.Email);
        // AccountResponse has no HashedPassword member at all, so the absence of a password hash
        // in the session payload is a compile-time guarantee, not a runtime assertion.
        Assert.Single(_unitOfWork.CommitAsyncCalls);
    }

    // -- RegisterAsync --------------------------------------------------------

    /// <summary>Verifies that <c>RegisterAsync</c> creates account and sends email when email does not exist.</summary>
    [Fact]
    public async Task RegisterAsync_CreatesAccountAndSendsEmail_WhenEmailDoesNotExist()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));
        _passwordService.AnswerHashPassword(_ => "$2a$13$hashed");
        _accountRepo.AnswerCreateAsync((_, _) => Task.CompletedTask);
        _dataProtection.AnswerProtect((kind, _) => kind == ProtectedValue.MailToken, (_, _) => "enc-token");
        SetupMailSuccess();
        var sut = CreateSut();

        var request = new RegisterAccountRequest
        {
            Email = "new@example.com",
            PlainPassword = "PlainPass1!",
            Nickname = "TestUser"
        };

        RegisterResult result = await sut.RegisterAsync(request, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.True(result.ShowResendEmail);
        Assert.Single(_accountRepo.CreateAsyncCalls);
    }

    /// <summary>A new registration mails the token itself and stores only its hash, never the token.</summary>
    [Fact]
    public async Task RegisterAsync_MailsTheToken_AndStoresOnlyItsHash()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));
        _passwordService.AnswerHashPassword(_ => "$2a$13$hashed");
        Account? created = null;
        _accountRepo.AnswerCreateAsync((a, _) => { created = a; return Task.CompletedTask; });
        string? mailed = null;
        _dataProtection.AnswerProtect((kind, _) => kind == ProtectedValue.MailToken, (_, t) => { mailed = t; return "enc-token"; });
        SetupMailSuccess();

        RegisterResult result = await CreateSut().RegisterAsync(
            new RegisterAccountRequest { Email = "new@example.com", PlainPassword = "PlainPass1!", Nickname = "TestUser" },
            _emailTemplate, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.NotNull(mailed);
        Assert.True(GuidToken.IsTokenAlive(mailed, Now));
        Assert.Equal(GuidToken.Hash(mailed), created?.RegistrationToken);
        Assert.Single(_accountRepo.UpdateRegistrationTokenAsyncCalls, call => call.Email == "new@example.com" && call.RegistrationToken == GuidToken.Hash(mailed));
    }

    /// <summary>
    /// Regression test (privilege escalation): self-registration always creates an unconfirmed
    /// <see cref="Role.User"/> account with a fresh registration token — the request type has no
    /// role, lock, confirmation or deletion field for a caller to set.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_AlwaysCreatesAnUnconfirmedUserAccount()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));
        _passwordService.AnswerHashPassword(_ => "$2a$13$hashed");
        Account? created = null;
        _accountRepo.AnswerCreateAsync((a, _) => { created = a; return Task.CompletedTask; });
        _dataProtection.AnswerProtect((kind, _) => kind == ProtectedValue.MailToken, (_, _) => "enc-token");
        SetupMailSuccess();

        var request = new RegisterAccountRequest
        {
            Email = "new@example.com",
            PlainPassword = "PlainPass1!",
            Nickname = "TestUser",
            TimeZoneIanaId = "UTC",
            AgreedServiceTerms = true
        };

        RegisterResult result = await CreateSut().RegisterAsync(request, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.NotNull(created);
        Assert.Equal(Role.User, created.Role.Value);
        Assert.False(created.Locked);
        Assert.False(created.EmailConfirmed);
        Assert.False(created.Deleted);
        Assert.Matches("^[0-9a-f]{64}$", created.RegistrationToken); // a SHA-256 hash, never the mailed token
    }

    /// <summary>
    /// Verifies that <c>RegisterAsync</c> rejects a reserved nickname (a staff / system name, or the
    /// anonymous placeholder) before doing any of the expensive or persisting work.
    /// </summary>
    [Theory]
    [InlineData("Admin")]
    [InlineData("login")]
    [InlineData("A d m i n")]
    public async Task RegisterAsync_ReturnsFail_WhenNicknameIsReserved(string nickname)
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));
        var sut = CreateSut();

        RegisterResult result = await sut.RegisterAsync(
            new RegisterAccountRequest { Email = "new@example.com", PlainPassword = "PlainPass1!", Nickname = nickname },
            _emailTemplate, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("'{0}' cannot be used as a Nickname.", result.ErrorKey);
        Assert.Empty(_passwordService.HashPasswordCalls);
        Assert.Empty(_accountRepo.CreateAsyncCalls);
    }

    /// <summary>Verifies that <c>RegisterAsync</c> rejects a nickname containing invisible characters.</summary>
    [Fact]
    public async Task RegisterAsync_ReturnsFail_WhenNicknameHasInvisibleCharacters()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));
        var sut = CreateSut();

        RegisterResult result = await sut.RegisterAsync(
            new RegisterAccountRequest { Email = "new@example.com", PlainPassword = "PlainPass1!", Nickname = "Bo\u200Bb" },
            _emailTemplate, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("Nickname contains characters that are not allowed.", result.ErrorKey);
        Assert.Empty(_accountRepo.CreateAsyncCalls);
    }

    /// <summary>
    /// Verifies that <c>RegisterAsync</c> reports a nickname already taken ignoring case (the DB
    /// constraint is case-sensitive, so this check is what stops "bob" registering next to "Bob"),
    /// and does so before hashing the password.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_ReturnsFail_WhenNicknameExistsIgnoringCase()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));
        _accountRepo.AnswerNicknameExistsIgnoreCaseAsync((nicknameArg, _) => nicknameArg == "bob", (_, _) => Task.FromResult<bool>(true));
        var sut = CreateSut();

        RegisterResult result = await sut.RegisterAsync(
            new RegisterAccountRequest { Email = "new@example.com", PlainPassword = "PlainPass1!", Nickname = "  bob " },
            _emailTemplate, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("'{0}' is a Nickname that already exists. Please enter another Nickname.", result.ErrorKey);
        Assert.Equal(["bob"], result.ErrorArgs);
        Assert.Empty(_passwordService.HashPasswordCalls);
        Assert.Empty(_accountRepo.CreateAsyncCalls);
    }

    /// <summary>
    /// Verifies that a concurrent "bob" / "Bob" registration — passing the app-level check but caught
    /// by the case-insensitive unique index — is reported as a nickname conflict, not an email one.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_ReturnsNicknameConflict_WhenCaseInsensitiveIndexRaces()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));
        _passwordService.AnswerHashPassword(_ => "$2a$13$hashed");
        _accountRepo.AnswerCreateAsync((_, _) => Task.FromException(new Exception("duplicate key", new Exception("23505: duplicate key value violates unique constraint \"Account_unique_index_0\""))));
        var sut = CreateSut();

        RegisterResult result = await sut.RegisterAsync(
            new RegisterAccountRequest { Email = "new@example.com", PlainPassword = "PlainPass1!", Nickname = "bob" },
            _emailTemplate, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("'{0}' is a Nickname that already exists. Please enter another Nickname.", result.ErrorKey);
        Assert.Equal(["bob"], result.ErrorArgs);
    }

    /// <summary>Verifies that <c>RegisterAsync</c> persists the normalized nickname, not the raw input.</summary>
    [Fact]
    public async Task RegisterAsync_StoresNormalizedNickname()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));
        _passwordService.AnswerHashPassword(_ => "$2a$13$hashed");
        Account? created = null;
        _accountRepo.AnswerCreateAsync((a, _) => { created = a; return Task.CompletedTask; });
        _dataProtection.AnswerProtect((kind, _) => kind == ProtectedValue.MailToken, (_, _) => "enc-token");
        SetupMailSuccess();
        var sut = CreateSut();

        RegisterResult result = await sut.RegisterAsync(
            new RegisterAccountRequest { Email = "new@example.com", PlainPassword = "PlainPass1!", Nickname = "  Ｂｏｂ   Smith " },
            _emailTemplate, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.NotNull(created);
        Assert.Equal("Bob Smith", created.Nickname);
    }

    /// <summary>Verifies that <c>RegisterAsync</c> stamps the published email with the ambient
    /// <see cref="Activity.Current"/> id, so Worker-side logs for that message can be traced
    /// back to this request.</summary>
    [Fact]
    public async Task RegisterAsync_PublishesEmail_WithCurrentActivityId()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));
        _passwordService.AnswerHashPassword(_ => "$2a$13$hashed");
        _accountRepo.AnswerCreateAsync((_, _) => Task.CompletedTask);
        _dataProtection.AnswerProtect((kind, _) => kind == ProtectedValue.MailToken, (_, _) => "enc-token");
        SetupMailSuccess();
        using var activity = new Activity("test-request");
        activity.Start();
        var sut = CreateSut();

        var request = new RegisterAccountRequest
        {
            Email = "new@example.com",
            PlainPassword = "PlainPass1!",
            Nickname = "TestUser"
        };

        await sut.RegisterAsync(request, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.Single(_emailPublisher.PublishAsyncCalls, call => (call.Message.CorrelationId == activity.Id));
    }

    /// <summary>Verifies that <c>RegisterAsync</c> returns fail when email sending fails.</summary>
    [Fact]
    public async Task RegisterAsync_ReturnsFail_WhenEmailSendingFails()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));
        _passwordService.AnswerHashPassword(_ => "$2a$13$hashed");
        _accountRepo.AnswerCreateAsync((_, _) => Task.CompletedTask);
        _accountRepo.AnswerUpdateEntityAsync((_, _) => Task.CompletedTask);
        _dataProtection.AnswerProtect((kind, _) => kind == ProtectedValue.MailToken, (_, _) => "enc-token");
        SetupMailFailure();
        var sut = CreateSut();

        var request = new RegisterAccountRequest
        {
            Email = "new@example.com",
            PlainPassword = "PlainPass1!"
        };

        RegisterResult result = await sut.RegisterAsync(request, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
    }

    /// <summary>
    /// Regression test: a brand-new account is already durably committed via <c>CreateAsync</c>
    /// before this call, with no surrounding try/catch in <c>RegisterAsync</c>. If building the
    /// confirmation mail body (including the protection of the registration token) throws, it
    /// must be caught and turned into a graceful <see cref="RegisterResult.Fail"/> rather than
    /// propagating as a raw unhandled exception out of <c>RegisterAsync</c>.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_ReturnsFail_WhenProtectingTheTokenThrows()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));
        _passwordService.AnswerHashPassword(_ => "$2a$13$hashed");
        _accountRepo.AnswerCreateAsync((_, _) => Task.CompletedTask);
        _dataProtection.AnswerProtect((kind, _) => kind == ProtectedValue.MailToken, (_, _) => throw new InvalidOperationException("key misconfigured"));
        var sut = CreateSut();

        var request = new RegisterAccountRequest
        {
            Email = "new@example.com",
            PlainPassword = "PlainPass1!",
            Nickname = "TestUser"
        };

        RegisterResult result = await sut.RegisterAsync(request, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Empty(_emailPublisher.PublishAsyncCalls);
    }

    /// <summary>Verifies that <c>RegisterAsync</c> returns fail when nickname already exists.</summary>
    [Fact]
    public async Task RegisterAsync_ReturnsFail_WhenNicknameAlreadyExists()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));
        _passwordService.AnswerHashPassword(_ => "$2a$13$hashed");
        // Simulate a unique constraint violation (Npgsql error code 23505) on the Nickname constraint specifically.
        _accountRepo.AnswerCreateAsync((_, _) => Task.FromException(new Exception("duplicate key", new Exception("23505: duplicate key value violates unique constraint \"Account_Nickname_unique\""))));
        var sut = CreateSut();

        var request = new RegisterAccountRequest
        {
            Email = "new@example.com",
            PlainPassword = "PlainPass1!",
            Nickname = "TakenNick"
        };

        RegisterResult result = await sut.RegisterAsync(request, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("Nickname", result.ErrorKey, StringComparison.Ordinal);
        Assert.Contains("TakenNick", result.ErrorArgs);
    }

    /// <summary>
    /// Regression test: two concurrent registrations for the same not-yet-registered email can both
    /// pass the prior <c>FindByEmailAsync</c> check and race on <c>CreateAsync</c>. The loser hits the
    /// Email primary-key violation (<c>Account_pk</c>), not the Nickname constraint, and must be told
    /// the email is taken — not incorrectly blamed on the nickname.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_ReturnsEmailAlreadyExists_WhenEmailPrimaryKeyViolationRaces()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));
        _passwordService.AnswerHashPassword(_ => "$2a$13$hashed");
        // Simulate a unique constraint violation on the Email primary key, not the Nickname constraint.
        _accountRepo.AnswerCreateAsync((_, _) => Task.FromException(new Exception("duplicate key", new Exception("23505: duplicate key value violates unique constraint \"Account_pk\""))));
        var sut = CreateSut();

        var request = new RegisterAccountRequest
        {
            Email = "raced@example.com",
            PlainPassword = "PlainPass1!",
            Nickname = "SomeNick"
        };

        RegisterResult result = await sut.RegisterAsync(request, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("raced@example.com", result.ErrorArgs);
        Assert.DoesNotContain("Nickname", result.ErrorKey, StringComparison.Ordinal);
    }

    /// <summary>Verifies that <c>RegisterAsync</c> resends confirmation email when account exists but not confirmed.</summary>
    [Fact]
    public async Task RegisterAsync_ResendsConfirmationEmail_WhenAccountExistsButNotConfirmed()
    {
        string token = GuidToken.Generate(Now);
        var existing = ActiveAccount(emailConfirmed: false, registrationToken: GuidToken.Hash(token));
        SetupUnconfirmedAccountForRegister(existing, lockedRead: existing);
        _dataProtection.AnswerProtect((kind, _) => kind == ProtectedValue.MailToken, (_, _) => "enc-token");
        SetupMailSuccess();
        var sut = CreateSut();

        RegisterResult result = await sut.RegisterAsync(NewRegistration(), _emailTemplate, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.True(result.ShowResendEmail);
        Assert.Single(_emailPublisher.PublishAsyncCalls);
    }

    /// <summary>Verifies that <c>RegisterAsync</c> returns fail when account already confirmed.</summary>
    [Fact]
    public async Task RegisterAsync_ReturnsFail_WhenAccountAlreadyConfirmed()
    {
        var existing = ActiveAccount(emailConfirmed: true);
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(existing));
        _accountRepo.AnswerUpdateEntityAsync((_, _) => Task.CompletedTask);
        var sut = CreateSut();

        var request = new RegisterAccountRequest { Email = existing.Email.Value, PlainPassword = "pw" };
        RegisterResult result = await sut.RegisterAsync(request, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("already", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Regression test (L-2): the "already confirmed" branch runs off an unauthenticated,
    /// address-only request. Without the caller proving ownership (current password), it must NOT
    /// mutate the row — no AgreedServiceTerms / Message write on a stranger's account.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_ConfirmedAccount_DoesNotTouchTheRow_WhenPasswordIsWrong()
    {
        var existing = ActiveAccount(emailConfirmed: true, agreedServiceTerms: false);
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(existing));
        _passwordService.AnswerVerifyPassword((_, _) => false);
        var sut = CreateSut();

        var request = new RegisterAccountRequest { Email = existing.Email.Value, PlainPassword = "not-the-password" };
        RegisterResult result = await sut.RegisterAsync(request, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("already", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(_accountRepo.UpdateAgreedServiceTermsAsyncCalls);
        Assert.Empty(_accountRepo.UpdateMessageAsyncCalls);
    }

    /// <summary>
    /// Companion to <see cref="RegisterAsync_ConfirmedAccount_DoesNotTouchTheRow_WhenPasswordIsWrong"/>:
    /// the legit owner re-running registration (correct password) still recovers a legacy
    /// "confirmed but terms not accepted" account.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_ConfirmedAccount_ReconcilesTerms_WhenPasswordMatches()
    {
        var existing = ActiveAccount(emailConfirmed: true, agreedServiceTerms: false);
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(existing));
        _accountRepo.AnswerUpdateAgreedServiceTermsAsync((_, _, _, _) => Task.FromResult<int>(1));
        _accountRepo.AnswerUpdateMessageAsync((_, _, _, _) => Task.FromResult<int>(1));
        _passwordService.AnswerVerifyPassword((_, _) => true);
        var sut = CreateSut();

        var request = new RegisterAccountRequest { Email = existing.Email.Value, PlainPassword = "correct" };
        RegisterResult result = await sut.RegisterAsync(request, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Single(_accountRepo.UpdateAgreedServiceTermsAsyncCalls, call => call.Email == existing.Email.Value && call.AgreedServiceTerms == true);
    }

    // -- ResendConfirmationEmailAsync ------------------------------------------

    /// <summary>
    /// Regression test: this endpoint is address-only (the resend form has no password field), so
    /// an already-confirmed account must never be written to -- otherwise anyone who knows a
    /// registered address could churn a stranger's account row for free, the same class of issue
    /// RegisterAsync's equivalent branch was hardened against.
    /// </summary>
    [Fact]
    public async Task ResendConfirmationEmailAsync_ConfirmedAccount_DoesNotTouchTheRow()
    {
        var existing = ActiveAccount(emailConfirmed: true);
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(existing));
        var sut = CreateSut();

        RegisterResult result = await sut.ResendConfirmationEmailAsync(existing.Email.Value, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("already", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(_accountRepo.UpdateMessageAsyncCalls);
    }

    // -- ConfirmEmailAsync ----------------------------------------------------

    /// <summary>Verifies that <c>ConfirmEmailAsync</c> returns invalid token when decryption fails.</summary>
    [Fact]
    public async Task ConfirmEmailAsync_ReturnsInvalidToken_WhenDecryptionFails()
    {
        _dataProtection.AnswerUnprotect((kind, _) => kind == ProtectedValue.MailToken, (_, _) => throw new Exception("bad cipher"));
        var sut = CreateSut();

        ConfirmEmailResult result = await sut.ConfirmEmailAsync("bad-encrypted-token", ConfirmPassword, TestContext.Current.CancellationToken);

        Assert.True(result.InvalidToken);
        Assert.False(result.AccountCreated);
    }

    /// <summary>Verifies that <c>ConfirmEmailAsync</c> returns invalid token when account not found.</summary>
    [Fact]
    public async Task ConfirmEmailAsync_ReturnsInvalidToken_WhenAccountNotFound()
    {
        string freshToken = GuidToken.Generate(Now);
        _dataProtection.AnswerUnprotect((kind, protectedValue) => kind == ProtectedValue.MailToken && protectedValue == "enc", (_, _) => freshToken);
        _accountRepo.AnswerFindByRegistrationTokenAsync((_, _) => Task.FromResult<Account?>(null));
        var sut = CreateSut();

        ConfirmEmailResult result = await sut.ConfirmEmailAsync("enc", ConfirmPassword, TestContext.Current.CancellationToken);

        Assert.True(result.InvalidToken);
        Assert.False(result.AccountCreated);
    }

    /// <summary>Verifies that <c>ConfirmEmailAsync</c> returns invalid token when token is expired.</summary>
    [Fact]
    public async Task ConfirmEmailAsync_ReturnsInvalidToken_WhenTokenIsExpired()
    {
        string expiredToken = BuildExpiredToken();
        _dataProtection.AnswerUnprotect((kind, protectedValue) => kind == ProtectedValue.MailToken && protectedValue == "enc-expired", (_, _) => expiredToken);
        _accountRepo.AnswerFindByRegistrationTokenAsync((_, _) => Task.FromResult<Account?>(ActiveAccount()));
        var sut = CreateSut();

        ConfirmEmailResult result = await sut.ConfirmEmailAsync("enc-expired", ConfirmPassword, TestContext.Current.CancellationToken);

        Assert.True(result.InvalidToken);
        Assert.False(result.AccountCreated);
    }

    /// <summary>Verifies that <c>ConfirmEmailAsync</c> confirms account when token is valid.</summary>
    [Fact]
    public async Task ConfirmEmailAsync_ConfirmsAccount_WhenTokenIsValid()
    {
        string freshToken = GuidToken.Generate(Now);
        var account = ActiveAccount(emailConfirmed: false, registrationToken: GuidToken.Hash(freshToken));
        _dataProtection.AnswerUnprotect((kind, protectedValue) => kind == ProtectedValue.MailToken && protectedValue == "enc", (_, _) => freshToken);
        _accountRepo.AnswerFindByRegistrationTokenAsync((_, _) => Task.FromResult<Account?>(account));
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(account));
        _accountRepo.AnswerUpdateEmailConfirmationAsync((_, _, _, _, _) => Task.FromResult<int>(1));
        var sut = CreateSut();

        ConfirmEmailResult result = await sut.ConfirmEmailAsync("enc", ConfirmPassword, TestContext.Current.CancellationToken);

        Assert.False(result.InvalidToken);
        Assert.True(result.AccountCreated);
        Assert.Single(_accountRepo.UpdateEmailConfirmationAsyncCalls, call => call.Email == account.Email.Value && call.RegistrationToken == null);
    }

    /// <summary>
    /// Regression: <c>UpdateEmailConfirmationAsync</c> is an <c>ExecuteUpdateAsync</c>-based,
    /// column-scoped write that matches zero rows -- without throwing -- when the account row was
    /// deleted concurrently between the <c>FindByRegistrationTokenAsync</c> lookup and this write.
    /// <c>ConfirmEmailAsync</c> must treat that as a failed confirmation (mirroring the "account not
    /// found" branch) rather than reporting <c>AccountCreated = true</c> for write that persisted
    /// nothing.
    /// </summary>
    [Fact]
    public async Task ConfirmEmailAsync_ReturnsInvalidTokenNotCreated_WhenAccountVanishesBeforeWrite()
    {
        string freshToken = GuidToken.Generate(Now);
        var account = ActiveAccount(emailConfirmed: false, registrationToken: GuidToken.Hash(freshToken));
        _dataProtection.AnswerUnprotect((kind, protectedValue) => kind == ProtectedValue.MailToken && protectedValue == "enc", (_, _) => freshToken);
        _accountRepo.AnswerFindByRegistrationTokenAsync((_, _) => Task.FromResult<Account?>(account));
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(account));
        _accountRepo.AnswerUpdateEmailConfirmationAsync((_, _, _, _, _) => Task.FromResult<int>(0));
        var sut = CreateSut();

        ConfirmEmailResult result = await sut.ConfirmEmailAsync("enc", ConfirmPassword, TestContext.Current.CancellationToken);

        Assert.True(result.InvalidToken);
        Assert.False(result.AccountCreated);
    }

    /// <summary>
    /// Verifies that <c>ConfirmEmailAsync</c> never falls back to a full-row <c>UpdateEntityAsync</c>
    /// write on the success path — it must use the column-scoped <c>UpdateEmailConfirmationAsync</c>
    /// so a concurrent write to another column on the same account row is not silently reverted by a
    /// stale full-row snapshot read from the un-locked token lookup.
    /// </summary>
    [Fact]
    public async Task ConfirmEmailAsync_DoesNotUseFullRowUpdate_WhenTokenIsValid()
    {
        string freshToken = GuidToken.Generate(Now);
        var account = ActiveAccount(emailConfirmed: false, registrationToken: GuidToken.Hash(freshToken));
        _dataProtection.AnswerUnprotect((kind, protectedValue) => kind == ProtectedValue.MailToken && protectedValue == "enc", (_, _) => freshToken);
        _accountRepo.AnswerFindByRegistrationTokenAsync((_, _) => Task.FromResult<Account?>(account));
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(account));
        _accountRepo.AnswerUpdateEmailConfirmationAsync((_, _, _, _, _) => Task.FromResult<int>(1));
        var sut = CreateSut();

        await sut.ConfirmEmailAsync("enc", ConfirmPassword, TestContext.Current.CancellationToken);

        Assert.Empty(_accountRepo.UpdateEntityAsyncCalls);
    }

    /// <summary>Verifies that <c>ConfirmEmailAsync</c> returns not created when account already confirmed.</summary>
    [Fact]
    public async Task ConfirmEmailAsync_ReturnsNotCreated_WhenAccountAlreadyConfirmed()
    {
        string freshToken = GuidToken.Generate(Now);
        var account = ActiveAccount(emailConfirmed: true);
        _dataProtection.AnswerUnprotect((kind, protectedValue) => kind == ProtectedValue.MailToken && protectedValue == "enc", (_, _) => freshToken);
        _accountRepo.AnswerFindByRegistrationTokenAsync((_, _) => Task.FromResult<Account?>(account));
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(account));
        _accountRepo.AnswerUpdateMessageAsync((_, _, _, _) => Task.FromResult<int>(1));
        var sut = CreateSut();

        ConfirmEmailResult result = await sut.ConfirmEmailAsync("enc", ConfirmPassword, TestContext.Current.CancellationToken);

        Assert.False(result.InvalidToken);
        Assert.False(result.AccountCreated);
    }

    // -- ForgotPasswordAsync --------------------------------------------------

    /// <summary>Verifies that <c>ForgotPasswordAsync</c> returns true silently when account not found.</summary>
    [Fact]
    public async Task ForgotPasswordAsync_ReturnsTrueSilently_WhenAccountNotFound()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));
        var sut = CreateSut();

        bool result = await sut.ForgotPasswordAsync("ghost@example.com", _emailTemplate, TestContext.Current.CancellationToken);

        Assert.True(result);
        Assert.Empty(_emailPublisher.PublishAsyncCalls);
    }

    /// <summary>Verifies that <c>ForgotPasswordAsync</c> returns true silently when account email not confirmed.</summary>
    [Fact]
    public async Task ForgotPasswordAsync_ReturnsTrueSilently_WhenAccountEmailNotConfirmed()
    {
        var account = ActiveAccount(emailConfirmed: false);
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(account));
        var sut = CreateSut();

        bool result = await sut.ForgotPasswordAsync(account.Email.Value, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.True(result);
        Assert.Empty(_emailPublisher.PublishAsyncCalls);
        VerifyNoWriteToRegisteredRow(account.Email.Value);
    }

    /// <summary>Verifies that <c>ForgotPasswordAsync</c> returns true silently for a deleted account and leaves its row alone.</summary>
    [Fact]
    public async Task ForgotPasswordAsync_ReturnsTrueSilently_AndLeavesRowAlone_WhenAccountDeleted()
    {
        var account = ActiveAccount(deleted: true);
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(account));
        var sut = CreateSut();

        bool result = await sut.ForgotPasswordAsync(account.Email.Value, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.True(result);
        Assert.Empty(_emailPublisher.PublishAsyncCalls);
        VerifyNoWriteToRegisteredRow(account.Email.Value);
    }

    /// <summary>
    /// The timing-equalization write of the "silently succeed" branch must never target the address the
    /// (anonymous) caller typed: for a registered-but-unconfirmed/deleted account that would rewrite a
    /// stranger's <c>Updated</c> stamp and clear its token. It still runs — against an address no
    /// account has — so the branch costs what the real path costs.
    /// </summary>
    private void VerifyNoWriteToRegisteredRow(string registeredEmail)
    {
        Assert.DoesNotContain(_accountRepo.UpdateResetPasswordTokenAsyncCalls, call => call.Email == registeredEmail);
        Assert.Single(_accountRepo.UpdateResetPasswordTokenAsyncCalls, call => call.Email == "" && call.ResetPasswordToken == null);
        Assert.DoesNotContain(_accountRepo.UpdateMessageAsyncCalls, call => call.Email == registeredEmail);
        Assert.Empty(_accountRepo.UpdateEntityAsyncCalls);
    }

    /// <summary>Verifies that <c>ForgotPasswordAsync</c> sends reset email when account is valid.</summary>
    [Fact]
    public async Task ForgotPasswordAsync_SendsResetEmail_WhenAccountIsValid()
    {
        var account = ActiveAccount();
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(account));
        _accountRepo.AnswerUpdateResetPasswordTokenAsync((_, _, _, _) => Task.FromResult<int>(1));
        _accountRepo.AnswerUpdateMessageAsync((_, _, _, _) => Task.FromResult<int>(1));
        _dataProtection.AnswerProtect((kind, _) => kind == ProtectedValue.MailToken, (_, _) => "enc-reset-token");
        SetupMailSuccess();
        var sut = CreateSut();

        bool result = await sut.ForgotPasswordAsync(account.Email.Value, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.True(result);
        // Column-scoped writes (not a full-row UpdateEntityAsync): the reset token first, then the
        // "mail sent" status message.
        Assert.Single(_accountRepo.UpdateResetPasswordTokenAsyncCalls, call => call.Email == account.Email.Value && (call.ResetPasswordToken != null));
        Assert.Single(_accountRepo.UpdateMessageAsyncCalls, call => call.Email == account.Email.Value);
        Assert.Empty(_accountRepo.UpdateEntityAsyncCalls);
    }

    /// <summary>The reset mail carries the token itself; the row stores only its hash.</summary>
    [Fact]
    public async Task ForgotPasswordAsync_MailsTheToken_AndStoresOnlyItsHash()
    {
        var account = ActiveAccount();
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(account));
        string? mailed = null;
        _dataProtection.AnswerProtect((kind, _) => kind == ProtectedValue.MailToken, (_, t) => { mailed = t; return "enc-reset-token"; });
        SetupMailSuccess();

        Assert.True(await CreateSut().ForgotPasswordAsync(account.Email.Value, _emailTemplate, TestContext.Current.CancellationToken));

        Assert.NotNull(mailed);
        Assert.True(GuidToken.IsTokenAlive(mailed, Now));
        Assert.Single(_accountRepo.UpdateResetPasswordTokenAsyncCalls, call => call.Email == account.Email.Value && call.ResetPasswordToken == GuidToken.Hash(mailed));
    }

    /// <summary>Verifies that <c>ForgotPasswordAsync</c> still sends a reset email when the account is locked,
    /// since resetting the password is the intended way to recover from a login lockout.</summary>
    [Fact]
    public async Task ForgotPasswordAsync_SendsResetEmail_WhenAccountIsLocked()
    {
        var account = ActiveAccount(locked: true);
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(account));
        _accountRepo.AnswerUpdateResetPasswordTokenAsync((_, _, _, _) => Task.FromResult<int>(1));
        _accountRepo.AnswerUpdateMessageAsync((_, _, _, _) => Task.FromResult<int>(1));
        _dataProtection.AnswerProtect((kind, _) => kind == ProtectedValue.MailToken, (_, _) => "enc-reset-token");
        SetupMailSuccess();
        var sut = CreateSut();

        bool result = await sut.ForgotPasswordAsync(account.Email.Value, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.True(result);
        Assert.Single(_accountRepo.UpdateResetPasswordTokenAsyncCalls, call => call.Email == account.Email.Value && (call.ResetPasswordToken != null));
        Assert.Single(_accountRepo.UpdateMessageAsyncCalls, call => call.Email == account.Email.Value);
        Assert.Empty(_accountRepo.UpdateEntityAsyncCalls);
    }

    // -- ValidateResetPasswordTokenAsync -------------------------------------

    /// <summary>Verifies that <c>ValidateResetPasswordTokenAsync</c> returns fail to reset when decryption fails.</summary>
    [Fact]
    public async Task ValidateResetPasswordTokenAsync_ReturnsFailToReset_WhenDecryptionFails()
    {
        _dataProtection.AnswerUnprotect((kind, _) => kind == ProtectedValue.MailToken, (_, _) => throw new Exception("bad cipher"));
        var sut = CreateSut();

        ResetPasswordValidationResult result = await sut.ValidateResetPasswordTokenAsync("bad", TestContext.Current.CancellationToken);

        Assert.True(result.FailToReset);
    }

    /// <summary>Verifies that <c>ValidateResetPasswordTokenAsync</c> returns fail to reset when token expired.</summary>
    [Fact]
    public async Task ValidateResetPasswordTokenAsync_ReturnsFailToReset_WhenTokenExpired()
    {
        string expiredToken = BuildExpiredToken();
        _dataProtection.AnswerUnprotect((kind, protectedValue) => kind == ProtectedValue.MailToken && protectedValue == "enc", (_, _) => expiredToken);
        _accountRepo.AnswerFindByResetPasswordTokenAsync((_, _) => Task.FromResult<Account?>(ActiveAccount()));
        var sut = CreateSut();

        ResetPasswordValidationResult result = await sut.ValidateResetPasswordTokenAsync("enc", TestContext.Current.CancellationToken);

        Assert.True(result.FailToReset);
    }

    /// <summary>Verifies that <c>ValidateResetPasswordTokenAsync</c> returns token when valid.</summary>
    [Fact]
    public async Task ValidateResetPasswordTokenAsync_ReturnsToken_WhenValid()
    {
        string freshToken = GuidToken.Generate(Now);
        _dataProtection.AnswerUnprotect((kind, protectedValue) => kind == ProtectedValue.MailToken && protectedValue == "enc", (_, _) => freshToken);
        _accountRepo.AnswerFindByResetPasswordTokenAsync((_, _) => Task.FromResult<Account?>(ActiveAccount()));
        var sut = CreateSut();

        ResetPasswordValidationResult result = await sut.ValidateResetPasswordTokenAsync("enc", TestContext.Current.CancellationToken);

        Assert.False(result.FailToReset);
        Assert.Equal("enc", result.ResetPasswordToken);
    }

    // -- ResetPasswordAsync ---------------------------------------------------

    /// <summary>Verifies that <c>ResetPasswordAsync</c> returns fail when decryption fails.</summary>
    [Fact]
    public async Task ResetPasswordAsync_ReturnsFail_WhenDecryptionFails()
    {
        _dataProtection.AnswerUnprotect((kind, _) => kind == ProtectedValue.MailToken, (_, _) => throw new Exception("bad cipher"));
        var sut = CreateSut();

        (ServiceResult result, _) = await sut.ResetPasswordAsync("bad", "newpass", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>ResetPasswordAsync</c> returns fail when token expired.</summary>
    [Fact]
    public async Task ResetPasswordAsync_ReturnsFail_WhenTokenExpired()
    {
        string expiredToken = BuildExpiredToken();
        _dataProtection.AnswerUnprotect((kind, protectedValue) => kind == ProtectedValue.MailToken && protectedValue == "enc", (_, _) => expiredToken);
        _accountRepo.AnswerFindByResetPasswordTokenAsync((_, _) => Task.FromResult<Account?>(ActiveAccount()));
        var sut = CreateSut();

        (ServiceResult result, _) = await sut.ResetPasswordAsync("enc", "newpass", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>ResetPasswordAsync</c> updates password and unlocks account when valid.</summary>
    [Fact]
    public async Task ResetPasswordAsync_UpdatesPasswordAndUnlocksAccount_WhenValid()
    {
        string freshToken = GuidToken.Generate(Now);
        var account = ActiveAccount(locked: true, loginAttempt: 3, resetPasswordToken: GuidToken.Hash(freshToken));
        _dataProtection.AnswerUnprotect((kind, protectedValue) => kind == ProtectedValue.MailToken && protectedValue == "enc", (_, _) => freshToken);
        _accountRepo.AnswerFindByResetPasswordTokenAsync((_, _) => Task.FromResult<Account?>(account));
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(account));
        _passwordService.AnswerHashPassword(passwordArg => passwordArg == "NewPass1!", _ => "$2a$13$newhash");
        _accountRepo.AnswerUpdateEntityAsync((_, _) => Task.CompletedTask);
        var sut = CreateSut();

        (ServiceResult result, _) = await sut.ResetPasswordAsync("enc", "NewPass1!", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Single(_accountRepo.UpdateEntityAsyncCalls, call => (call.Domain!.HashedPassword == "$2a$13$newhash" &&
            call.Domain!.Locked == false &&
            call.Domain!.LoginAttempt == 0 &&
            call.Domain!.ResetPasswordToken == null));
    }

    /// <summary>
    /// A reset that succeeds hands back the account to sign in with: unlocked, with the NEW security stamp (a
    /// session carrying the old one would be thrown out on its next request) and no forced password change.
    /// </summary>
    [Fact]
    public async Task ResetPasswordAsync_ReturnsTheAccountToSignIn_WithItsNewSecurityStamp()
    {
        string freshToken = GuidToken.Generate(Now);
        var account = ActiveAccount(email: "reset@example.com", locked: true, loginAttempt: 3, resetPasswordToken: GuidToken.Hash(freshToken));
        string oldStamp = account.SecurityStamp;
        _dataProtection.AnswerUnprotect((kind, protectedValue) => kind == ProtectedValue.MailToken && protectedValue == "enc", (_, _) => freshToken);
        _accountRepo.AnswerFindByResetPasswordTokenAsync((_, _) => Task.FromResult<Account?>(account));
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(account));
        _passwordService.AnswerHashPassword(passwordArg => passwordArg == "NewPass1!", _ => "$2a$13$newhash");
        _accountRepo.AnswerUpdateEntityAsync((_, _) => Task.CompletedTask);

        (ServiceResult result, AccountResponse? signIn) = await CreateSut().ResetPasswordAsync("enc", "NewPass1!", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.NotNull(signIn);
        Assert.Equal("reset@example.com", signIn.Email);
        Assert.NotEqual(oldStamp, signIn.SecurityStamp);
        Assert.Equal(account.SecurityStamp, signIn.SecurityStamp);
        Assert.False(signIn.Locked);
        Assert.False(signIn.MustChangePassword);
    }

    /// <summary>
    /// An account that has not accepted the service terms gets its new password but is not signed in: LoginAsync
    /// refuses it too, so a reset must not be a way around that.
    /// </summary>
    [Fact]
    public async Task ResetPasswordAsync_ReturnsNoAccountToSignIn_WhenTheServiceTermsAreNotAccepted()
    {
        string freshToken = GuidToken.Generate(Now);
        var account = ActiveAccount(agreedServiceTerms: false, resetPasswordToken: GuidToken.Hash(freshToken));
        _dataProtection.AnswerUnprotect((kind, protectedValue) => kind == ProtectedValue.MailToken && protectedValue == "enc", (_, _) => freshToken);
        _accountRepo.AnswerFindByResetPasswordTokenAsync((_, _) => Task.FromResult<Account?>(account));
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(account));
        _passwordService.AnswerHashPassword(passwordArg => passwordArg == "NewPass1!", _ => "$2a$13$newhash");
        _accountRepo.AnswerUpdateEntityAsync((_, _) => Task.CompletedTask);

        (ServiceResult result, AccountResponse? signIn) = await CreateSut().ResetPasswordAsync("enc", "NewPass1!", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Null(signIn);
    }

    // -- GetAccountsByNicknamesAsync ------------------------------------------

    /// <summary>Verifies that <c>GetAccountsByNicknamesAsync</c> delegates to repository.</summary>
    [Fact]
    public async Task GetAccountsByNicknamesAsync_DelegatesToRepository()
    {
        var nicknames = new[] { "Alice", "Bob" };
        var expected = new List<Account>
        {
            ActiveAccount("alice@example.com"),
            ActiveAccount("bob@example.com")
        };
        _accountRepo.AnswerFindByNicknamesAsync((_, _) => Task.FromResult<List<Account>>(expected));

        var result = await CreateSut().GetAccountsByNicknamesAsync(nicknames, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.Single(_accountRepo.FindByNicknamesAsyncCalls);
    }

    // -- Concurrency / failure branches ----------------------------------------

    /// <summary>A registration request with the given nickname and password.</summary>
    private static RegisterAccountRequest NewRegistration(string nickname = "TestUser", string password = "PlainPass1!") => new()
    {
        Email = "new@example.com",
        PlainPassword = password,
        Nickname = nickname
    };

    /// <summary>Hash returned for the new password when an unconfirmed registration is re-submitted.</summary>
    private const string ReplacementHash = "$2a$13$replacement";

    /// <summary>Arranges a re-registration: the plain read returns <paramref name="existing"/>, the locked re-read returns <paramref name="lockedRead"/>, and hashing yields <see cref="ReplacementHash"/>.</summary>
    private void SetupUnconfirmedAccountForRegister(Account existing, Account? lockedRead)
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(existing));
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(lockedRead));
        _passwordService.AnswerHashPassword(_ => ReplacementHash);
    }

    /// <summary>Verifies that <c>GetAccountByEmailAsync</c> returns null for an unknown address and a mapped response otherwise.</summary>
    [Fact]
    public async Task GetAccountByEmailAsync_ReturnsNullWhenMissing_AndMappedResponseWhenFound()
    {
        var known = ActiveAccount("known@example.com");
        _accountRepo.AnswerFindByEmailAsync((email, _) => email == "known@example.com", (_, _) => Task.FromResult<Account?>(known));
        _accountRepo.AnswerFindByEmailAsync((email, _) => email == "ghost@example.com", (_, _) => Task.FromResult<Account?>(null));
        var sut = CreateSut();

        Assert.Null(await sut.GetAccountByEmailAsync("ghost@example.com", TestContext.Current.CancellationToken));
        Assert.Equal("known@example.com", (await sut.GetAccountByEmailAsync("known@example.com", TestContext.Current.CancellationToken))?.Email);
    }

    /// <summary>An unexpected repository failure during login must release the row lock (rollback) and propagate.</summary>
    [Fact]
    public async Task LoginAsync_RollsBackAndRethrows_WhenTheRepositoryThrows()
    {
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromException<Account?>(new InvalidOperationException("db down")));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut().LoginAsync("user@example.com", "pw", TestContext.Current.CancellationToken));

        Assert.Single(_unitOfWork.RollbackAsyncCalls);
        Assert.Empty(_unitOfWork.CommitAsyncCalls);
    }

    /// <summary>The complexity rule is enforced at the service boundary, before the (slow) hash and before anything is stored.</summary>
    [Fact]
    public async Task RegisterAsync_ReturnsPasswordViolation_AndDoesNotHashOrCreate_WhenPasswordIsTooWeak()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));

        RegisterResult result = await CreateSut().RegisterAsync(NewRegistration(password: "weak"), _emailTemplate, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal(PasswordPolicy.ViolationMessage, result.ErrorKey);
        Assert.Empty(_passwordService.HashPasswordCalls);
        Assert.Empty(_accountRepo.CreateAsyncCalls);
    }

    /// <summary>A failure that is not a unique-key race is logged and reported as a generic registration error.</summary>
    [Fact]
    public async Task RegisterAsync_ReturnsGenericError_WhenCreateFailsForAnyOtherReason()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));
        _passwordService.AnswerHashPassword(_ => "$2a$13$hashed");
        _accountRepo.AnswerCreateAsync((_, _) => Task.FromException(new InvalidOperationException("connection reset")));

        RegisterResult result = await CreateSut().RegisterAsync(NewRegistration(), _emailTemplate, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("Error occurred while processing about account registration", result.ErrorKey);
        Assert.Empty(_emailPublisher.PublishAsyncCalls);
    }

    /// <summary>The account vanishing between the unlocked read and the FOR UPDATE re-read is reported, not dereferenced.</summary>
    [Fact]
    public async Task RegisterAsync_Unconfirmed_ReturnsGenericError_WhenTheRowVanishesUnderTheLock()
    {
        SetupUnconfirmedAccountForRegister(ActiveAccount(emailConfirmed: false), lockedRead: null);

        RegisterResult result = await CreateSut().RegisterAsync(NewRegistration(), _emailTemplate, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("Error occurred while processing about account registration", result.ErrorKey);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
        Assert.Empty(_emailPublisher.PublishAsyncCalls);
    }

    /// <summary>A concurrent confirmation that wins the lock turns the re-registration into "already exists", with no mail.</summary>
    [Fact]
    public async Task RegisterAsync_Unconfirmed_ReportsAlreadyCreated_WhenConfirmedConcurrentlyWhileWaitingForTheLock()
    {
        SetupUnconfirmedAccountForRegister(ActiveAccount(emailConfirmed: false), lockedRead: ActiveAccount(emailConfirmed: true));

        RegisterResult result = await CreateSut().RegisterAsync(NewRegistration(), _emailTemplate, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal(AccountMessage.UserAlreadyCreated.GetDescription(), result.ErrorKey);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
        Assert.Empty(_emailPublisher.PublishAsyncCalls);
    }

    /// <summary>
    /// An expired registration token is regenerated on re-registration so the mailed link is alive; the mail carries the
    /// fresh token and the row only its hash.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_Unconfirmed_RegeneratesAnExpiredRegistrationToken()
    {
        string expired = BuildExpiredToken();
        var existing = ActiveAccount(emailConfirmed: false, registrationToken: GuidToken.Hash(expired));
        SetupUnconfirmedAccountForRegister(existing, lockedRead: existing);
        string? mailed = null;
        _dataProtection.AnswerProtect((kind, _) => kind == ProtectedValue.MailToken, (_, t) => { mailed = t; return "enc-token"; });
        SetupMailSuccess();

        RegisterResult result = await CreateSut().RegisterAsync(NewRegistration(), _emailTemplate, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.NotNull(mailed);
        Assert.NotEqual(expired, mailed);
        Assert.True(GuidToken.IsTokenAlive(mailed, Now));
        Assert.Contains(_accountRepo.UpdateRegistrationTokenAsyncCalls, call => call.Email == existing.Email.Value && call.RegistrationToken == GuidToken.Hash(mailed));
    }

    /// <summary>A failing write of the replaced registration under the lock releases it and propagates instead of mailing a dead link.</summary>
    [Fact]
    public async Task RegisterAsync_Unconfirmed_RollsBackAndRethrows_WhenPersistingTheReplacementFails()
    {
        var existing = ActiveAccount(emailConfirmed: false, registrationToken: GuidToken.Generate(Now));
        SetupUnconfirmedAccountForRegister(existing, lockedRead: existing);
        _accountRepo.AnswerUpdateEntityAsync((_, _) => Task.FromException(new InvalidOperationException("db down")));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut().RegisterAsync(NewRegistration(), _emailTemplate, TestContext.Current.CancellationToken));

        Assert.Single(_unitOfWork.RollbackAsyncCalls);
        Assert.Empty(_emailPublisher.PublishAsyncCalls);
    }

    /// <summary>
    /// Pre-hijacking guard: re-registering an address nobody has confirmed replaces the earlier
    /// registrant's password, nickname and time zone with the new submission and issues a fresh
    /// token, so the confirmation mail the owner receives activates the owner's credentials.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_Unconfirmed_ReplacesTheEarlierRegistrantsCredentials()
    {
        string squatterToken = GuidToken.Generate(Now);
        var existing = ActiveAccount(emailConfirmed: false, registrationToken: GuidToken.Hash(squatterToken)); // nickname "User", time zone "UTC"
        SetupUnconfirmedAccountForRegister(existing, lockedRead: existing);
        _dataProtection.AnswerProtect((kind, _) => kind == ProtectedValue.MailToken, (_, _) => "enc-token");
        SetupMailSuccess();
        Account? saved = null;
        _accountRepo.AnswerUpdateEntityAsync((a, _) => { saved = a; return Task.CompletedTask; });

        var request = NewRegistration(nickname: "Owner", password: "OwnerPass1!");
        request.TimeZoneIanaId = "Asia/Seoul";
        RegisterResult result = await CreateSut().RegisterAsync(request, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Single(_passwordService.HashPasswordCalls, call => call == "OwnerPass1!");
        Assert.NotNull(saved);
        Assert.Equal(ReplacementHash, saved.HashedPassword);
        Assert.Equal("Owner", saved.Nickname);
        Assert.Equal("Asia/Seoul", saved.TimeZone.Value);
        Assert.NotEqual(GuidToken.Hash(squatterToken), saved.RegistrationToken);
        Assert.Matches("^[0-9a-f]{64}$", saved.RegistrationToken);
        Assert.Single(_unitOfWork.CommitAsyncCalls);
        Assert.Single(_emailPublisher.PublishAsyncCalls);
    }

    /// <summary>Re-registering with a nickname another account already holds is refused before anything is written or mailed.</summary>
    [Fact]
    public async Task RegisterAsync_Unconfirmed_ReturnsNicknameConflict_WhenAnotherAccountHoldsTheNickname()
    {
        var existing = ActiveAccount(emailConfirmed: false, registrationToken: GuidToken.Generate(Now));
        SetupUnconfirmedAccountForRegister(existing, lockedRead: existing);
        _accountRepo.AnswerNicknameExistsIgnoreCaseAsync((nicknameArg, _) => nicknameArg == "Taken", (_, _) => Task.FromResult<bool>(true));

        RegisterResult result = await CreateSut().RegisterAsync(NewRegistration(nickname: "Taken"), _emailTemplate, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("'{0}' is a Nickname that already exists. Please enter another Nickname.", result.ErrorKey);
        Assert.Equal(["Taken"], result.ErrorArgs);
        Assert.Empty(_accountRepo.UpdateEntityAsyncCalls);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
        Assert.Empty(_emailPublisher.PublishAsyncCalls);
    }

    /// <summary>
    /// The account's own current nickname is not a conflict: keeping it (or changing only its case)
    /// must not be refused by the uniqueness check that the account itself would trip.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_Unconfirmed_KeepsItsOwnNickname_WithoutAConflictCheck()
    {
        var existing = ActiveAccount(emailConfirmed: false, registrationToken: GuidToken.Generate(Now)); // nickname "User"
        SetupUnconfirmedAccountForRegister(existing, lockedRead: existing);
        _accountRepo.AnswerNicknameExistsIgnoreCaseAsync((_, _) => Task.FromResult<bool>(true));
        _dataProtection.AnswerProtect((kind, _) => kind == ProtectedValue.MailToken, (_, _) => "enc-token");
        SetupMailSuccess();

        RegisterResult result = await CreateSut().RegisterAsync(NewRegistration(nickname: "user"), _emailTemplate, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal("user", existing.Nickname);
        Assert.Empty(_accountRepo.NicknameExistsIgnoreCaseAsyncCalls);
    }

    /// <summary>A concurrent registration taking the same nickname surfaces at commit as the unique index; it is reported as a nickname conflict.</summary>
    [Fact]
    public async Task RegisterAsync_Unconfirmed_ReturnsNicknameConflict_WhenTheUniqueIndexRacesAtCommit()
    {
        var existing = ActiveAccount(emailConfirmed: false, registrationToken: GuidToken.Generate(Now));
        SetupUnconfirmedAccountForRegister(existing, lockedRead: existing);
        _unitOfWork.AnswerCommitAsync(_ => Task.FromException(new Exception("duplicate key", new Exception("23505: duplicate key value violates unique constraint \"Account_unique_index_0\""))));

        RegisterResult result = await CreateSut().RegisterAsync(NewRegistration(nickname: "Racer"), _emailTemplate, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("'{0}' is a Nickname that already exists. Please enter another Nickname.", result.ErrorKey);
        Assert.Equal(["Racer"], result.ErrorArgs);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
        Assert.Empty(_emailPublisher.PublishAsyncCalls);
    }

    /// <summary>
    /// A time zone the domain refuses surfaces only under the lock, when the earlier registration is replaced: the transaction is
    /// rolled back, the domain's error is returned and nothing is written or mailed.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_Unconfirmed_RollsBackWithTheDomainError_WhenTheTimeZoneIsUnknown()
    {
        var existing = ActiveAccount(emailConfirmed: false, registrationToken: GuidToken.Generate(Now));
        SetupUnconfirmedAccountForRegister(existing, lockedRead: existing);
        RegisterAccountRequest request = NewRegistration();
        request.TimeZoneIanaId = "Not/AZone";

        RegisterResult result = await CreateSut().RegisterAsync(request, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("'{0}' is not a recognised time-zone ID.", result.ErrorKey);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
        Assert.Empty(_unitOfWork.CommitAsyncCalls);
        Assert.Empty(_accountRepo.UpdateEntityAsyncCalls);
        Assert.Empty(_emailPublisher.PublishAsyncCalls);
    }

    /// <summary>A weak password or an invalid nickname is refused before hashing or taking the row lock.</summary>
    [Theory]
    [InlineData("TestUser", "weak")]
    [InlineData("admin", "PlainPass1!")]
    public async Task RegisterAsync_Unconfirmed_RefusesInvalidInput_BeforeHashingOrLocking(string nickname, string password)
    {
        var existing = ActiveAccount(emailConfirmed: false, registrationToken: GuidToken.Generate(Now));
        SetupUnconfirmedAccountForRegister(existing, lockedRead: existing);

        RegisterResult result = await CreateSut().RegisterAsync(NewRegistration(nickname, password), _emailTemplate, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Empty(_passwordService.HashPasswordCalls);
        Assert.Empty(_accountRepo.FindByEmailForUpdateAsyncCalls);
    }

    /// <summary>A failure while reconciling a confirmed account's status is logged and reported as a status error.</summary>
    [Fact]
    public async Task RegisterAsync_ConfirmedAccount_ReportsStatusError_WhenReconcilingTheRowFails()
    {
        var existing = ActiveAccount(emailConfirmed: true, agreedServiceTerms: false);
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(existing));
        _passwordService.AnswerVerifyPassword((_, _) => true);
        _accountRepo.AnswerUpdateAgreedServiceTermsAsync((_, _, _, _) => Task.FromException<int>(new InvalidOperationException("db down")));

        RegisterResult result = await CreateSut().RegisterAsync(NewRegistration(), _emailTemplate, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("Error occurred while processing about account status", result.ErrorKey);
    }

    /// <summary>Resending for an unknown address asks the UI to keep showing the resend button.</summary>
    [Fact]
    public async Task ResendConfirmationEmailAsync_ReturnsFailWithResendButton_WhenAccountNotFound()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));

        RegisterResult result = await CreateSut().ResendConfirmationEmailAsync("ghost@example.com", _emailTemplate, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.True(result.ShowResendEmail);
        Assert.Equal("Failed to resend email", result.ErrorKey);
    }

    /// <summary>The row vanishing under the lock is reported like "not found" and the lock is released.</summary>
    [Fact]
    public async Task ResendConfirmationEmailAsync_ReturnsFail_WhenTheRowVanishesUnderTheLock()
    {
        SetupUnconfirmedAccountForRegister(ActiveAccount(emailConfirmed: false), lockedRead: null);

        RegisterResult result = await CreateSut().ResendConfirmationEmailAsync("user@example.com", _emailTemplate, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.True(result.ShowResendEmail);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>A concurrent confirmation that wins the lock stops the resend with "already exists" and sends nothing.</summary>
    [Fact]
    public async Task ResendConfirmationEmailAsync_ReportsAlreadyCreated_WhenConfirmedConcurrentlyWhileWaitingForTheLock()
    {
        SetupUnconfirmedAccountForRegister(ActiveAccount(emailConfirmed: false), lockedRead: ActiveAccount(emailConfirmed: true));

        RegisterResult result = await CreateSut().ResendConfirmationEmailAsync("user@example.com", _emailTemplate, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal(AccountMessage.UserAlreadyCreated.GetDescription(), result.ErrorKey);
        Assert.Empty(_emailPublisher.PublishAsyncCalls);
    }

    /// <summary>
    /// A resend always mints a fresh token, even while the stored one is still live: only its hash is stored, so the
    /// earlier token cannot be mailed again. The mail carries the fresh token, the row its hash, and the outcome is a repeat.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ResendConfirmationEmailAsync_MailsAFreshToken_AndStoresOnlyItsHash(bool storedTokenIsLive)
    {
        string stored = storedTokenIsLive ? GuidToken.Generate(Now) : BuildExpiredToken();
        var account = ActiveAccount(emailConfirmed: false, registrationToken: GuidToken.Hash(stored));
        SetupUnconfirmedAccountForRegister(account, lockedRead: account);
        string? mailed = null;
        _dataProtection.AnswerProtect((kind, _) => kind == ProtectedValue.MailToken, (_, t) => { mailed = t; return "enc-token"; });
        SetupMailSuccess();

        RegisterResult result = await CreateSut().ResendConfirmationEmailAsync(account.Email.Value, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.True(result.RepeatEmailSend);
        Assert.NotNull(mailed);
        Assert.NotEqual(stored, mailed);
        Assert.True(GuidToken.IsTokenAlive(mailed, Now));
        Assert.Contains(_accountRepo.UpdateRegistrationTokenAsyncCalls, call => call.Email == account.Email.Value && call.RegistrationToken == GuidToken.Hash(mailed));
        Assert.DoesNotContain(_accountRepo.UpdateRegistrationTokenAsyncCalls, call => call.Email == account.Email.Value && (call.RegistrationToken != GuidToken.Hash(mailed)));
    }

    /// <summary>A failing token write under the lock releases it and propagates.</summary>
    [Fact]
    public async Task ResendConfirmationEmailAsync_RollsBackAndRethrows_WhenPersistingTheTokenFails()
    {
        var account = ActiveAccount(emailConfirmed: false, registrationToken: GuidToken.Generate(Now));
        SetupUnconfirmedAccountForRegister(account, lockedRead: account);
        _accountRepo.AnswerUpdateRegistrationTokenAsync((_, _, _, _) => Task.FromException<int>(new InvalidOperationException("db down")));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut().ResendConfirmationEmailAsync(account.Email.Value, _emailTemplate, TestContext.Current.CancellationToken));

        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>The confirmation-mail send reports a status error (and sends nothing) when the token cannot be persisted first.</summary>
    [Fact]
    public async Task RegisterAsync_NewAccount_ReportsStatusError_WhenThePostCreateTokenWriteFails()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));
        _passwordService.AnswerHashPassword(_ => "$2a$13$hashed");
        _accountRepo.AnswerCreateAsync((_, _) => Task.CompletedTask);
        _accountRepo.AnswerUpdateRegistrationTokenAsync((_, _, _, _) => Task.FromException<int>(new InvalidOperationException("db down")));

        RegisterResult result = await CreateSut().RegisterAsync(NewRegistration(), _emailTemplate, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("Error occurred while processing about account status", result.ErrorKey);
        Assert.Empty(_emailPublisher.PublishAsyncCalls);
    }

    /// <summary>A failing final "verify e-mail" status write after a successful send is reported as a status error.</summary>
    [Fact]
    public async Task ResendConfirmationEmailAsync_ReportsStatusError_WhenTheFinalStatusWriteFails()
    {
        var account = ActiveAccount(emailConfirmed: false, registrationToken: GuidToken.Generate(Now));
        SetupUnconfirmedAccountForRegister(account, lockedRead: account);
        _dataProtection.AnswerProtect((kind, _) => kind == ProtectedValue.MailToken, (_, _) => "enc-token");
        SetupMailSuccess();
        _accountRepo.AnswerUpdateMessageAsync((_, _, _, _) => Task.FromException<int>(new InvalidOperationException("db down")));

        RegisterResult result = await CreateSut().ResendConfirmationEmailAsync(account.Email.Value, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("Error occurred while processing about account status", result.ErrorKey);
    }

    /// <summary>A mail-send failure whose "FailToMailSent" status write also fails is still reported as a send failure.</summary>
    [Fact]
    public async Task ResendConfirmationEmailAsync_ReportsSendFailure_EvenWhenRecordingTheFailureAlsoFails()
    {
        var account = ActiveAccount(emailConfirmed: false, registrationToken: GuidToken.Generate(Now));
        SetupUnconfirmedAccountForRegister(account, lockedRead: account);
        _dataProtection.AnswerProtect((kind, _) => kind == ProtectedValue.MailToken, (_, _) => "enc-token");
        SetupMailFailure();
        _accountRepo.AnswerUpdateMessageAsync((_, _, _, _) => Task.FromException<int>(new InvalidOperationException("db down")));

        RegisterResult result = await CreateSut().ResendConfirmationEmailAsync(account.Email.Value, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("Error occurred while processing about sending account authentication mail", result.ErrorKey);
    }

    // -- ConfirmEmailAsync: registration password -------------------------------

    /// <summary>
    /// The link alone does not activate the account: a password that does not match the one chosen at
    /// registration leaves it unconfirmed, keeps the token for a retry, and writes nothing.
    /// </summary>
    [Fact]
    public async Task ConfirmEmailAsync_ReturnsWrongPassword_AndConfirmsNothing_WhenThePasswordDoesNotMatch()
    {
        string token = GuidToken.Generate(Now);
        var account = ActiveAccount(emailConfirmed: false, registrationToken: GuidToken.Hash(token));
        SetupConfirmLookup(account, account, token);
        _passwordService.AnswerVerifyPassword((passwordArg, hashedPassword) => passwordArg == "Not-The-Password1!" && hashedPassword == account.HashedPassword, (_, _) => false);

        ConfirmEmailResult result = await CreateSut().ConfirmEmailAsync("enc", "Not-The-Password1!", TestContext.Current.CancellationToken);

        Assert.True(result.WrongPassword);
        Assert.False(result.InvalidToken);
        Assert.False(result.AccountCreated);
        Assert.Equal("enc", result.RegistrationToken);
        Assert.False(account.EmailConfirmed);
        Assert.Empty(_accountRepo.UpdateEmailConfirmationAsyncCalls);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>The registration password is checked against the locked row's hash — the credentials actually being activated.</summary>
    [Fact]
    public async Task ConfirmEmailAsync_ChecksThePasswordAgainstTheLockedRow()
    {
        string token = GuidToken.Generate(Now);
        var account = ActiveAccount(emailConfirmed: false, registrationToken: GuidToken.Hash(token));
        SetupConfirmLookup(account, account, token);
        _accountRepo.AnswerUpdateEmailConfirmationAsync((_, _, _, _, _) => Task.FromResult<int>(1));

        ConfirmEmailResult result = await CreateSut().ConfirmEmailAsync("enc", ConfirmPassword, TestContext.Current.CancellationToken);

        Assert.True(result.AccountCreated);
        Assert.Single(_passwordService.VerifyPasswordCalls, call => call.Password == ConfirmPassword && call.HashedPassword == account.HashedPassword);
    }

    // -- ValidateRegistrationTokenAsync --------------------------------------------

    /// <summary>A live link for an unconfirmed account shows the password form (its token is echoed back) and changes nothing.</summary>
    [Fact]
    public async Task ValidateRegistrationTokenAsync_ReturnsTheTokenForTheForm_WhenTheLinkIsLive()
    {
        string token = GuidToken.Generate(Now);
        _dataProtection.AnswerUnprotect((kindArg, protectedValue) => kindArg == ProtectedValue.MailToken && protectedValue == "enc", (_, _) => token);
        _accountRepo.AnswerFindByRegistrationTokenAsync((tokenArg, _) => tokenArg == GuidToken.Hash(token), (_, _) => Task.FromResult<Account?>(ActiveAccount(emailConfirmed: false, registrationToken: GuidToken.Hash(token))));

        ConfirmEmailResult result = await CreateSut().ValidateRegistrationTokenAsync("enc", TestContext.Current.CancellationToken);

        Assert.False(result.InvalidToken);
        Assert.False(result.AccountCreated);
        Assert.Equal("enc", result.RegistrationToken);
        Assert.Equal(0, _unitOfWork.TotalCallCount);
    }

    /// <summary>An undecryptable, unknown or expired link is invalid and shows no form.</summary>
    [Theory]
    [InlineData("undecryptable")]
    [InlineData("unknown")]
    [InlineData("expired")]
    public async Task ValidateRegistrationTokenAsync_ReturnsInvalidToken_WhenTheLinkIsDead(string kind)
    {
        string token = kind == "expired" ? BuildExpiredToken() : GuidToken.Generate(Now);
        if (kind == "undecryptable")
            _dataProtection.AnswerUnprotect((kindArg, protectedValue) => kindArg == ProtectedValue.MailToken && protectedValue == "enc", (_, _) => throw new FormatException("bad cipher"));
        else
            _dataProtection.AnswerUnprotect((kindArg, protectedValue) => kindArg == ProtectedValue.MailToken && protectedValue == "enc", (_, _) => token);
        _accountRepo.AnswerFindByRegistrationTokenAsync((tokenArg, _) => tokenArg == GuidToken.Hash(token), (_, _) => Task.FromResult<Account?>(kind == "unknown" ? null : ActiveAccount(emailConfirmed: false, registrationToken: GuidToken.Hash(token))));

        ConfirmEmailResult result = await CreateSut().ValidateRegistrationTokenAsync("enc", TestContext.Current.CancellationToken);

        Assert.True(result.InvalidToken);
        Assert.False(result.AccountCreated);
        Assert.Null(result.RegistrationToken);
    }

    // -- ConfirmEmailAsync: locked re-read branches ------------------------------

    /// <summary>Arranges e-mail confirmation: <c>"enc"</c> decrypts to <paramref name="rawToken"/>, the token lookup returns <paramref name="found"/>, and the locked re-read returns <paramref name="locked"/>.</summary>
    private void SetupConfirmLookup(Account found, Account? locked, string rawToken)
    {
        _dataProtection.AnswerUnprotect((kindArg, protectedValue) => kindArg == ProtectedValue.MailToken && protectedValue == "enc", (_, _) => rawToken);
        _accountRepo.AnswerFindByRegistrationTokenAsync((_, _) => Task.FromResult<Account?>(found));
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(locked));
    }

    /// <summary>The row vanishing between the token lookup and the FOR UPDATE re-read is an invalid token, nothing created.</summary>
    [Fact]
    public async Task ConfirmEmailAsync_ReturnsInvalidTokenNotCreated_WhenTheRowVanishesUnderTheLock()
    {
        string token = GuidToken.Generate(Now);
        SetupConfirmLookup(ActiveAccount(emailConfirmed: false, registrationToken: GuidToken.Hash(token)), locked: null, token);

        ConfirmEmailResult result = await CreateSut().ConfirmEmailAsync("enc", ConfirmPassword, TestContext.Current.CancellationToken);

        Assert.True(result.InvalidToken);
        Assert.False(result.AccountCreated);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>
    /// A token superseded by a concurrent resend (no longer matches the locked row) is answered like any other invalid link
    /// (InvalidToken, not AccountCreated), so the page shows the invalid-link message rather than the error state.
    /// </summary>
    [Fact]
    public async Task ConfirmEmailAsync_ReturnsInvalidToken_WhenTheTokenWasSupersededUnderTheLock()
    {
        string token = GuidToken.Generate(Now);
        var stale = ActiveAccount(emailConfirmed: false, registrationToken: GuidToken.Hash(token));
        var superseded = ActiveAccount(emailConfirmed: false, registrationToken: GuidToken.Generate(Now));
        SetupConfirmLookup(stale, superseded, token);

        ConfirmEmailResult result = await CreateSut().ConfirmEmailAsync("enc", ConfirmPassword, TestContext.Current.CancellationToken);

        Assert.True(result.InvalidToken);
        Assert.False(result.AccountCreated);
        Assert.Empty(_accountRepo.UpdateEmailConfirmationAsyncCalls);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>When the status write for an already-confirmed account fails, the transaction is rolled back and the token is still reported valid.</summary>
    [Fact]
    public async Task ConfirmEmailAsync_RollsBack_WhenWritingTheAlreadyConfirmedMessageFails()
    {
        string token = GuidToken.Generate(Now);
        var found = ActiveAccount(emailConfirmed: false, registrationToken: GuidToken.Hash(token));
        SetupConfirmLookup(found, ActiveAccount(emailConfirmed: true), token);
        _accountRepo.AnswerUpdateMessageAsync((_, _, _, _) => Task.FromException<int>(new InvalidOperationException("db down")));

        ConfirmEmailResult result = await CreateSut().ConfirmEmailAsync("enc", ConfirmPassword, TestContext.Current.CancellationToken);

        Assert.False(result.InvalidToken);
        Assert.False(result.AccountCreated);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
        Assert.Empty(_unitOfWork.CommitAsyncCalls);
    }

    /// <summary>A failing confirmation write is rolled back and surfaced with its error key so the page can explain.</summary>
    [Fact]
    public async Task ConfirmEmailAsync_ReturnsErrorKey_WhenTheConfirmationWriteThrows()
    {
        string token = GuidToken.Generate(Now);
        var account = ActiveAccount(emailConfirmed: false, registrationToken: GuidToken.Hash(token));
        SetupConfirmLookup(account, account, token);
        _accountRepo.AnswerUpdateEmailConfirmationAsync((_, _, _, _, _) => Task.FromException<int>(new InvalidOperationException("db down")));

        ConfirmEmailResult result = await CreateSut().ConfirmEmailAsync("enc", ConfirmPassword, TestContext.Current.CancellationToken);

        Assert.True(result.InvalidToken);
        Assert.True(result.AccountCreated);
        Assert.Equal("Error occurred while processing about confirm email account status", result.ErrorKey);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    /// <summary>An unexpected failure taking the lock releases it and propagates.</summary>
    [Fact]
    public async Task ConfirmEmailAsync_RollsBackAndRethrows_WhenTheLockedReadThrows()
    {
        string token = GuidToken.Generate(Now);
        _dataProtection.AnswerUnprotect((kind, protectedValue) => kind == ProtectedValue.MailToken && protectedValue == "enc", (_, _) => token);
        _accountRepo.AnswerFindByRegistrationTokenAsync((_, _) => Task.FromResult<Account?>(ActiveAccount(emailConfirmed: false, registrationToken: GuidToken.Hash(token))));
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromException<Account?>(new InvalidOperationException("db down")));

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSut().ConfirmEmailAsync("enc", ConfirmPassword, TestContext.Current.CancellationToken));

        Assert.Single(_unitOfWork.RollbackAsyncCalls);
    }

    // -- ForgotPasswordAsync / ResetPasswordAsync: failure branches ---------------

    /// <summary>The timing-equalization work of an unknown address never surfaces a failure to the caller.</summary>
    [Fact]
    public async Task ForgotPasswordAsync_StillReturnsTrue_WhenTheEqualizationWorkThrows()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));
        _dataProtection.AnswerProtect((kind, _) => kind == ProtectedValue.MailToken, (_, _) => throw new InvalidOperationException("data protection down"));

        bool result = await CreateSut().ForgotPasswordAsync("ghost@example.com", _emailTemplate, TestContext.Current.CancellationToken);

        Assert.True(result);
        Assert.Empty(_emailPublisher.PublishAsyncCalls);
    }

    /// <summary>A failure persisting the reset token is swallowed (enumeration-safe reply) and no mail is sent.</summary>
    [Fact]
    public async Task ForgotPasswordAsync_ReturnsTrueAndSendsNothing_WhenPersistingTheResetTokenFails()
    {
        var account = ActiveAccount();
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(account));
        _accountRepo.AnswerUpdateResetPasswordTokenAsync((_, _, _, _) => Task.FromException<int>(new InvalidOperationException("db down")));

        bool result = await CreateSut().ForgotPasswordAsync(account.Email.Value, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.True(result);
        Assert.Empty(_emailPublisher.PublishAsyncCalls);
    }

    /// <summary>A new password that breaks the complexity rule is refused before hashing, leaving the reset token usable.</summary>
    [Fact]
    public async Task ResetPasswordAsync_ReturnsPolicyViolation_AndDoesNotHash_WhenPasswordIsTooWeak()
    {
        string token = GuidToken.Generate(Now);
        var account = ActiveAccount(resetPasswordToken: GuidToken.Hash(token));
        _dataProtection.AnswerUnprotect((kind, protectedValue) => kind == ProtectedValue.MailToken && protectedValue == "enc", (_, _) => token);
        _accountRepo.AnswerFindByResetPasswordTokenAsync((_, _) => Task.FromResult<Account?>(account));

        (ServiceResult result, _) = await CreateSut().ResetPasswordAsync("enc", "weak", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Empty(_passwordService.HashPasswordCalls);
        Assert.DoesNotContain(_unitOfWork.BeginAsyncCalls, call => call.IsolationLevel == null);
    }

    /// <summary>The account vanishing (or being soft-deleted) between the token lookup and the lock refuses the reset.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResetPasswordAsync_ReturnsInvalid_WhenTheLockedRowIsMissingOrDeleted(bool rowExistsButDeleted)
    {
        string token = GuidToken.Generate(Now);
        var found = ActiveAccount(resetPasswordToken: GuidToken.Hash(token));
        _dataProtection.AnswerUnprotect((kind, protectedValue) => kind == ProtectedValue.MailToken && protectedValue == "enc", (_, _) => token);
        _accountRepo.AnswerFindByResetPasswordTokenAsync((_, _) => Task.FromResult<Account?>(found));
        _passwordService.AnswerHashPassword(_ => "$2a$13$newhash");
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(rowExistsButDeleted ? ActiveAccount(resetPasswordToken: GuidToken.Hash(token), deleted: true) : null));

        (ServiceResult result, _) = await CreateSut().ResetPasswordAsync("enc", "NewPass1!", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
        Assert.Empty(_accountRepo.UpdateEntityAsyncCalls);
    }

    /// <summary>A reset token consumed by a concurrent reset between the lookup and the lock is rejected (compare-and-swap).</summary>
    [Fact]
    public async Task ResetPasswordAsync_ReturnsInvalid_WhenTheTokenWasConsumedConcurrently()
    {
        string token = GuidToken.Generate(Now);
        var found = ActiveAccount(resetPasswordToken: GuidToken.Hash(token));
        var consumed = ActiveAccount(resetPasswordToken: null);
        _dataProtection.AnswerUnprotect((kind, protectedValue) => kind == ProtectedValue.MailToken && protectedValue == "enc", (_, _) => token);
        _accountRepo.AnswerFindByResetPasswordTokenAsync((_, _) => Task.FromResult<Account?>(found));
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(consumed));
        _passwordService.AnswerHashPassword(_ => "$2a$13$newhash");

        (ServiceResult result, _) = await CreateSut().ResetPasswordAsync("enc", "NewPass1!", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Empty(_accountRepo.UpdateEntityAsyncCalls);
    }

    /// <summary>A persistence failure during the reset is rolled back and reported with its own failure code.</summary>
    [Fact]
    public async Task ResetPasswordAsync_RollsBackAndReportsFailure_WhenTheWriteThrows()
    {
        string token = GuidToken.Generate(Now);
        var account = ActiveAccount(resetPasswordToken: GuidToken.Hash(token));
        _dataProtection.AnswerUnprotect((kind, protectedValue) => kind == ProtectedValue.MailToken && protectedValue == "enc", (_, _) => token);
        _accountRepo.AnswerFindByResetPasswordTokenAsync((_, _) => Task.FromResult<Account?>(account));
        _accountRepo.AnswerFindByEmailForUpdateAsync((_, _) => Task.FromResult<Account?>(account));
        _passwordService.AnswerHashPassword(_ => "$2a$13$newhash");
        _accountRepo.AnswerUpdateEntityAsync((_, _) => Task.FromException(new InvalidOperationException("db down")));

        (ServiceResult result, _) = await CreateSut().ResetPasswordAsync("enc", "NewPass1!", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
        Assert.Empty(_unitOfWork.CommitAsyncCalls);
    }

    // -- Email lookups ignore surrounding whitespace and letter case ------------------------

    /// <summary>
    /// The address typed for a login, password reset or confirmation resend is looked up in the
    /// stored (trimmed, lower-case) form, so a mobile autocomplete's trailing space still finds the account.
    /// </summary>
    [Fact]
    public async Task EmailLookups_UseTheTrimmedLowerCaseAddress()
    {
        const string typed = " User@Example.com\t";
        var sut = CreateSut();

        await sut.LoginAsync(typed, "pw", TestContext.Current.CancellationToken);
        await sut.ForgotPasswordAsync(typed, _emailTemplate, TestContext.Current.CancellationToken);
        await sut.ResendConfirmationEmailAsync(typed, _emailTemplate, TestContext.Current.CancellationToken);
        await sut.RegisterAsync(new RegisterAccountRequest { Email = typed, PlainPassword = "weak" }, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.Single(_accountRepo.FindByEmailForUpdateAsyncCalls, call => call.Email == "user@example.com");
        Assert.Equal(3, _accountRepo.FindByEmailAsyncCalls.Count(call => call.Email == "user@example.com"));
        Assert.DoesNotContain(_accountRepo.FindByEmailAsyncCalls, call => (call.Email != "user@example.com"));
        Assert.DoesNotContain(_accountRepo.FindByEmailForUpdateAsyncCalls, call => (call.Email != "user@example.com"));
    }

    // -- Private helpers ------------------------------------------------------

    /// <summary>A token minted 25 hours before <see cref="Now"/>, past its 24h validity window.</summary>
    private static string BuildExpiredToken() => GuidToken.Generate(Now.AddHours(-25));

    // -- Login transaction, reset/confirmation flows, account messages and logging -------------

    /// <summary>Asserts exactly one entry at <paramref name="level"/> reads <paramref name="message"/> and carries <paramref name="thrown"/> (or none).</summary>
    private void AssertLogged(LogLevel level, string message, Exception? thrown = null)
    {
        FakeLogRecord record = Assert.Single(_logger.Collector.GetSnapshot(), r => r.Level == level && r.Message == message);
        Assert.Same(thrown, record.Exception);
    }

    /// <summary>Arranges a confirmed account whose live reset token <c>"enc"</c> decrypts to; returns the account.</summary>
    private Account GivenAResettableAccount(bool locked = false)
    {
        string token = GuidToken.Generate(Now);
        Account account = ActiveAccount(locked: locked, loginAttempt: locked ? 5 : 0, resetPasswordToken: GuidToken.Hash(token));
        _dataProtection.AnswerUnprotect((kind, protectedValue) => kind == ProtectedValue.MailToken && protectedValue == "enc", (_, _) => token);
        _accountRepo.AnswerFindByResetPasswordTokenAsync((tokenArg, _) => tokenArg == GuidToken.Hash(token), (_, _) => Task.FromResult<Account?>(account));
        _accountRepo.AnswerFindByEmailForUpdateAsync((email, _) => email == account.Email.Value, (_, _) => Task.FromResult<Account?>(account));
        _passwordService.AnswerHashPassword(_ => "$2a$13$newhash");
        return account;
    }

    /// <summary>A successful login runs in its own transaction, resets the failed-attempt counter and persists it.</summary>
    [Fact]
    public async Task LoginAsync_ResetsAndPersistsTheFailedAttemptCounter_InACommittedTransaction()
    {
        Account account = ActiveAccount(loginAttempt: 3);
        _accountRepo.AnswerFindByEmailForUpdateAsync((email, _) => email == account.Email.Value, (_, _) => Task.FromResult<Account?>(account));
        _passwordService.AnswerVerifyPassword((passwordArg, hashedPassword) => passwordArg == "Right1234!" && hashedPassword == account.HashedPassword, (_, _) => true);

        LoginResult result = await CreateSut().LoginAsync(account.Email.Value, "Right1234!", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Single(_accountRepo.UpdateEntityAsyncCalls, call => (call.Domain!.LoginAttempt == 0));
        Assert.Single(_unitOfWork.BeginAsyncCalls, call => call.IsolationLevel == null);
        Assert.Single(_unitOfWork.CommitAsyncCalls);
    }

    /// <summary>An unknown address commits its equalizing (0-row) write, so it costs what a wrong password costs.</summary>
    [Fact]
    public async Task LoginAsync_CommitsTheEqualizingWrite_ForAnUnknownAddress()
    {
        await CreateSut().LoginAsync("ghost@example.com", "any", TestContext.Current.CancellationToken);

        Assert.Single(_accountRepo.UpdateMessageAsyncCalls, call => call.Email == "ghost@example.com" && call.Message == null && call.Updated == Now);
        Assert.Single(_unitOfWork.CommitAsyncCalls);
    }

    /// <summary>A refusal after the right password (deleted, unconfirmed, terms not accepted) writes nothing and is rolled back.</summary>
    [Theory]
    [InlineData(true, true, true)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    public async Task LoginAsync_RollsBack_ARefusalAfterTheRightPassword(bool deleted, bool emailConfirmed, bool agreedServiceTerms)
    {
        Account account = ActiveAccount(deleted: deleted, emailConfirmed: emailConfirmed, agreedServiceTerms: agreedServiceTerms);
        _accountRepo.AnswerFindByEmailForUpdateAsync((email, _) => email == account.Email.Value, (_, _) => Task.FromResult<Account?>(account));
        _passwordService.AnswerVerifyPassword((password, hashedPassword) => password == "Right1234!" && hashedPassword == account.HashedPassword, (_, _) => true);

        LoginResult result = await CreateSut().LoginAsync(account.Email.Value, "Right1234!", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
        Assert.Empty(_unitOfWork.CommitAsyncCalls);
    }

    /// <summary>An undecryptable registration link is logged as a Warning with the decryption failure.</summary>
    [Fact]
    public async Task ValidateRegistrationTokenAsync_LogsAWarning_WhenTheTokenDoesNotDecrypt()
    {
        var thrown = new FormatException("bad");
        _dataProtection.AnswerUnprotect((kind, protectedValue) => kind == ProtectedValue.MailToken && protectedValue == "enc", (_, _) => throw thrown);

        ConfirmEmailResult result = await CreateSut().ValidateRegistrationTokenAsync("enc", TestContext.Current.CancellationToken);

        Assert.True(result.InvalidToken);
        AssertLogged(LogLevel.Warning, "Failed to decrypt registration token", thrown);
    }

    /// <summary>A dead registration link is logged as a Warning.</summary>
    [Fact]
    public async Task ValidateRegistrationTokenAsync_LogsAWarning_WhenTheLinkIsDead()
    {
        _dataProtection.AnswerUnprotect((kind, protectedValue) => kind == ProtectedValue.MailToken && protectedValue == "enc", (_, _) => BuildExpiredToken());

        await CreateSut().ValidateRegistrationTokenAsync("enc", TestContext.Current.CancellationToken);

        AssertLogged(LogLevel.Warning, "Email confirmation link rejected: invalid or expired token");
    }

    /// <summary>A failure of the timing-equalization work for an unknown address is logged as a Warning.</summary>
    [Fact]
    public async Task ForgotPasswordAsync_LogsAWarning_WhenTheEqualizationWorkThrows()
    {
        var thrown = new InvalidOperationException("data protection down");
        _dataProtection.AnswerProtect((kind, _) => kind == ProtectedValue.MailToken, (_, _) => throw thrown);

        await CreateSut().ForgotPasswordAsync("ghost@example.com", _emailTemplate, TestContext.Current.CancellationToken);

        AssertLogged(LogLevel.Warning, "ForgotPassword equalization failed for ghost@example.com", thrown);
    }

    /// <summary>A failure while issuing the reset for a real account is logged as a Warning (the reply stays the same).</summary>
    [Fact]
    public async Task ForgotPasswordAsync_LogsAWarning_WhenIssuingTheResetFails()
    {
        Account account = ActiveAccount();
        _accountRepo.AnswerFindByEmailAsync((email, _) => email == account.Email.Value, (_, _) => Task.FromResult<Account?>(account));
        var thrown = new InvalidOperationException("db down");
        _accountRepo.AnswerUpdateResetPasswordTokenAsync((_, _, _, _) => Task.FromException<int>(thrown));

        await CreateSut().ForgotPasswordAsync(account.Email.Value, _emailTemplate, TestContext.Current.CancellationToken);

        AssertLogged(LogLevel.Warning, $"ForgotPassword failed for {account.Email.Value}", thrown);
    }

    /// <summary>
    /// The reset mail is built for the configured domain, and the account then records whether it went out ("Email has
    /// been sent…" or "Fail to mail sent").
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ForgotPasswordAsync_BuildsTheMailForTheDomain_AndRecordsWhetherItWentOut(bool published)
    {
        Account account = ActiveAccount();
        _accountRepo.AnswerFindByEmailAsync((email, _) => email == account.Email.Value, (_, _) => Task.FromResult<Account?>(account));
        _dataProtection.AnswerProtect((kind, _) => kind == ProtectedValue.MailToken, (_, _) => "enc-token");
        if (published) SetupMailSuccess(); else SetupMailFailure();

        await CreateSut().ForgotPasswordAsync(account.Email.Value, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.Single(_mailClient.GetMailResetPasswordBodyCalls, call => call.ResetPasswordToken == "enc-token" && call.Title == "title" && call.Content0 == "c0" && call.Content1 == "c1" && call.DomainName == "https://example.com");
        string expected = published ? AccountMessage.ResetPasswordMail.GetDescription() : "Fail to mail sent";
        Assert.Single(_accountRepo.UpdateMessageAsyncCalls, call => call.Email == account.Email.Value && call.Message == expected);
    }

    /// <summary>An undecryptable reset link is logged as a Warning and reported as dead.</summary>
    [Fact]
    public async Task ValidateResetPasswordTokenAsync_LogsAWarning_WhenTheTokenDoesNotDecrypt()
    {
        var thrown = new FormatException("bad");
        _dataProtection.AnswerUnprotect((kind, protectedValue) => kind == ProtectedValue.MailToken && protectedValue == "enc", (_, _) => throw thrown);

        await CreateSut().ValidateResetPasswordTokenAsync("enc", TestContext.Current.CancellationToken);

        AssertLogged(LogLevel.Warning, "Failed to decrypt reset password token", thrown);
    }

    /// <summary>A reset link that matches no account is dead (and logged as such).</summary>
    [Fact]
    public async Task ValidateResetPasswordTokenAsync_ReportsADeadLink_WhenNoAccountHoldsTheToken()
    {
        _dataProtection.AnswerUnprotect((kind, protectedValue) => kind == ProtectedValue.MailToken && protectedValue == "enc", (_, _) => GuidToken.Generate(Now));

        ResetPasswordValidationResult result = await CreateSut().ValidateResetPasswordTokenAsync("enc", TestContext.Current.CancellationToken);

        Assert.True(result.FailToReset);
        AssertLogged(LogLevel.Warning, "Password reset link rejected: invalid or expired token");
    }

    /// <summary>An undecryptable reset is refused as "reset-password-invalid" and logged.</summary>
    [Fact]
    public async Task ResetPasswordAsync_RefusesAndLogs_WhenTheTokenDoesNotDecrypt()
    {
        var thrown = new FormatException("bad");
        _dataProtection.AnswerUnprotect((kind, protectedValue) => kind == ProtectedValue.MailToken && protectedValue == "enc", (_, _) => throw thrown);

        (ServiceResult result, _) = await CreateSut().ResetPasswordAsync("enc", "NewPass1!", TestContext.Current.CancellationToken);

        Assert.Equal("reset-password-invalid", result.ErrorKey);
        AssertLogged(LogLevel.Warning, "Failed to decrypt reset password token", thrown);
    }

    /// <summary>A live token held by a deleted or unconfirmed account is refused as "reset-password-invalid", before any hashing.</summary>
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task ResetPasswordAsync_RefusesALiveTokenOfADeletedOrUnconfirmedAccount(bool deleted, bool emailConfirmed)
    {
        string token = GuidToken.Generate(Now);
        _dataProtection.AnswerUnprotect((kind, protectedValue) => kind == ProtectedValue.MailToken && protectedValue == "enc", (_, _) => token);
        _accountRepo.AnswerFindByResetPasswordTokenAsync((tokenArg, _) => tokenArg == GuidToken.Hash(token), (_, _) => Task.FromResult<Account?>(ActiveAccount(deleted: deleted, emailConfirmed: emailConfirmed, resetPasswordToken: GuidToken.Hash(token))));

        (ServiceResult result, _) = await CreateSut().ResetPasswordAsync("enc", "NewPass1!", TestContext.Current.CancellationToken);

        Assert.Equal("reset-password-invalid", result.ErrorKey);
        Assert.Empty(_passwordService.HashPasswordCalls);
    }

    /// <summary>A weak new password is the password-policy Validation error.</summary>
    [Fact]
    public async Task ResetPasswordAsync_ReturnsThePasswordPolicyError_ForAWeakPassword()
    {
        GivenAResettableAccount();

        (ServiceResult result, _) = await CreateSut().ResetPasswordAsync("enc", "weak", TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("Account.PasswordPolicy", result.ErrorCode);
        Assert.Equal(PasswordPolicy.ViolationMessage, result.ErrorKey);
    }

    /// <summary>A successful reset records "Success to reset password" on the account, in a committed transaction.</summary>
    [Fact]
    public async Task ResetPasswordAsync_RecordsTheSuccessMessage_InACommittedTransaction()
    {
        GivenAResettableAccount();

        (ServiceResult result, _) = await CreateSut().ResetPasswordAsync("enc", "NewPass1!", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Single(_accountRepo.UpdateEntityAsyncCalls, call => (call.Domain!.Message == "Success to reset password"));
        Assert.Single(_unitOfWork.BeginAsyncCalls, call => call.IsolationLevel == null);
        Assert.Single(_unitOfWork.CommitAsyncCalls);
    }

    /// <summary>A row that vanished under the lock is refused as "reset-password-invalid" and logged with the address.</summary>
    [Fact]
    public async Task ResetPasswordAsync_RefusesAndLogs_WhenTheRowVanishedUnderTheLock()
    {
        Account account = GivenAResettableAccount();
        _accountRepo.AnswerFindByEmailForUpdateAsync((email, _) => email == account.Email.Value, (_, _) => Task.FromResult<Account?>(null));

        (ServiceResult result, _) = await CreateSut().ResetPasswordAsync("enc", "NewPass1!", TestContext.Current.CancellationToken);

        Assert.Equal("reset-password-invalid", result.ErrorKey);
        AssertLogged(LogLevel.Warning, $"Password reset rejected: account {account.Email.Value} no longer exists or is deleted");
    }

    /// <summary>A token consumed concurrently is refused as "reset-password-invalid", rolled back, and logged with the address.</summary>
    [Fact]
    public async Task ResetPasswordAsync_RefusesRollsBackAndLogs_WhenTheTokenWasConsumedUnderTheLock()
    {
        Account found = GivenAResettableAccount();
        _accountRepo.AnswerFindByEmailForUpdateAsync((email, _) => email == found.Email.Value, (_, _) => Task.FromResult<Account?>(ActiveAccount(resetPasswordToken: null)));

        (ServiceResult result, _) = await CreateSut().ResetPasswordAsync("enc", "NewPass1!", TestContext.Current.CancellationToken);

        Assert.Equal("reset-password-invalid", result.ErrorKey);
        Assert.Single(_unitOfWork.RollbackAsyncCalls);
        AssertLogged(LogLevel.Warning, $"Password reset rejected: invalid or expired token for {found.Email.Value}");
    }

    /// <summary>A reset whose write throws is logged with the exception and reported with its own message.</summary>
    [Fact]
    public async Task ResetPasswordAsync_LogsAndReportsResetPasswordFailed_WhenTheWriteThrows()
    {
        Account account = GivenAResettableAccount();
        var thrown = new InvalidOperationException("db down");
        _accountRepo.AnswerUpdateEntityAsync((_, _) => Task.FromException(thrown));

        (ServiceResult result, _) = await CreateSut().ResetPasswordAsync("enc", "NewPass1!", TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorType.Failure, result.ErrorType);
        Assert.Equal("Account.ResetPasswordFailed", result.ErrorCode);
        Assert.Equal("Error occurred while processing about reset password", result.ErrorKey);
        AssertLogged(LogLevel.Error, $"Failed to reset password for {account.Email.Value}", thrown);
    }

    /// <summary>Arranges a resend for an unconfirmed account with a live token; returns the account.</summary>
    private Account GivenAResendableAccount()
    {
        Account account = ActiveAccount(emailConfirmed: false, registrationToken: GuidToken.Generate(Now));
        SetupUnconfirmedAccountForRegister(account, lockedRead: account);
        _dataProtection.AnswerProtect((kind, _) => kind == ProtectedValue.MailToken, (_, _) => "enc-token");
        return account;
    }

    /// <summary>A registration token that cannot be persisted before the mail is logged with the exception and the address.</summary>
    [Fact]
    public async Task RegisterAsync_LogsTheFailure_WhenThePostCreateTokenWriteFails()
    {
        _passwordService.AnswerHashPassword(_ => "$2a$13$hashed");
        var thrown = new InvalidOperationException("db down");
        _accountRepo.AnswerUpdateRegistrationTokenAsync((_, _, _, _) => Task.FromException<int>(thrown));

        await CreateSut().RegisterAsync(NewRegistration(), _emailTemplate, TestContext.Current.CancellationToken);

        AssertLogged(LogLevel.Error, "Failed to persist registration token for new@example.com", thrown);
    }

    /// <summary>The confirmation mail is built for the configured domain.</summary>
    [Fact]
    public async Task ResendConfirmationEmailAsync_BuildsTheMailForTheDomain()
    {
        Account account = GivenAResendableAccount();
        SetupMailSuccess();

        await CreateSut().ResendConfirmationEmailAsync(account.Email.Value, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.Single(_mailClient.GetMailConfirmationBodyCalls, call => call.RegistrationToken == "enc-token" && call.Title == "title" && call.Content0 == "c0" && call.Content1 == "c1" && call.DomainName == "https://example.com");
    }

    /// <summary>A confirmation mail body that cannot be built is logged and reported as a send failure.</summary>
    [Fact]
    public async Task ResendConfirmationEmailAsync_LogsAndReportsASendFailure_WhenTheMailBodyCannotBeBuilt()
    {
        Account account = GivenAResendableAccount();
        var thrown = new InvalidOperationException("data protection down");
        _dataProtection.AnswerProtect((kind, _) => kind == ProtectedValue.MailToken, (_, _) => throw thrown);

        RegisterResult result = await CreateSut().ResendConfirmationEmailAsync(account.Email.Value, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.Equal("Error occurred while processing about sending account authentication mail", result.ErrorKey);
        AssertLogged(LogLevel.Error, $"Failed to build confirmation mail body for {account.Email.Value}", thrown);
    }

    /// <summary>
    /// A confirmation mail that cannot be queued records "Fail to mail sent" on the account, and the queue failure is
    /// logged with the address.
    /// </summary>
    [Fact]
    public async Task ResendConfirmationEmailAsync_RecordsTheSendFailure_AndLogsIt()
    {
        Account account = GivenAResendableAccount();
        SetupMailFailure();

        await CreateSut().ResendConfirmationEmailAsync(account.Email.Value, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.Single(_accountRepo.UpdateMessageAsyncCalls, call => call.Email == account.Email.Value && call.Message == "Fail to mail sent");
        FakeLogRecord error = Assert.Single(_logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Error);
        Assert.Equal($"Failed to publish email message for {account.Email.Value}", error.Message);
        Assert.IsType<InvalidOperationException>(error.Exception);
    }

    /// <summary>Failing to record the send failure as well is logged as a Warning.</summary>
    [Fact]
    public async Task ResendConfirmationEmailAsync_LogsAWarning_WhenRecordingTheSendFailureAlsoFails()
    {
        Account account = GivenAResendableAccount();
        SetupMailFailure();
        var thrown = new InvalidOperationException("db down");
        _accountRepo.AnswerUpdateMessageAsync((_, _, _, _) => Task.FromException<int>(thrown));

        await CreateSut().ResendConfirmationEmailAsync(account.Email.Value, _emailTemplate, TestContext.Current.CancellationToken);

        AssertLogged(LogLevel.Warning, $"Failed to update account message after mail send failure for {account.Email.Value}", thrown);
    }

    /// <summary>A queued confirmation mail records "verify your mail" on the account, and the publish is logged with an empty correlation id outside a request.</summary>
    [Fact]
    public async Task ResendConfirmationEmailAsync_RecordsVerifyEmail_AndLogsThePublish()
    {
        Account account = GivenAResendableAccount();
        SetupMailSuccess();
        Activity.Current = null;

        await CreateSut().ResendConfirmationEmailAsync(account.Email.Value, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.Single(_accountRepo.UpdateMessageAsyncCalls, call => call.Email == account.Email.Value && call.Message == "User already created, please verify your given mail Id");
        AssertLogged(LogLevel.Information, $"Publishing email to {account.Email.Value}. CorrelationId=");
        Assert.Single(_emailPublisher.PublishAsyncCalls, call => (call.Message.CorrelationId == ""));
    }

    /// <summary>A failing final status write after a queued mail is logged with the exception and the address.</summary>
    [Fact]
    public async Task ResendConfirmationEmailAsync_LogsTheFailure_WhenTheFinalStatusWriteFails()
    {
        Account account = GivenAResendableAccount();
        SetupMailSuccess();
        var thrown = new InvalidOperationException("db down");
        _accountRepo.AnswerUpdateMessageAsync((_, _, _, _) => Task.FromException<int>(thrown));

        await CreateSut().ResendConfirmationEmailAsync(account.Email.Value, _emailTemplate, TestContext.Current.CancellationToken);

        AssertLogged(LogLevel.Error, $"Failed to update account status for {account.Email.Value}", thrown);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _unitOfWork.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
