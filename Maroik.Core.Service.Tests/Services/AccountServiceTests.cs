using System.Diagnostics;
using Maroik.Core.Domain.Account;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Misc.Enums;
using Maroik.Core.Contract.Misc.Helpers;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Misc.Messaging;
using Maroik.Core.Contract.Misc.Settings;
using Maroik.Core.Contract.Misc.Tokens;
using Maroik.Core.Service.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// Unit tests for <see cref="AccountService"/>.
/// All external dependencies (repository, password service, mail client, email publisher, RSA service)
/// are replaced with Moq mocks so tests run without a database, SMTP server, or RabbitMQ broker.
/// Covers login, registration, email confirmation, and password-reset flows.
/// </summary>
public class AccountServiceTests
{
    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly Mock<IPasswordService> _passwordService = new();
    private readonly Mock<IMailClient> _mailClient = new();
    private readonly Mock<IEmailPublisher> _emailPublisher = new();
    private readonly Mock<IRsaService> _rsa = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly IOptions<ServerSetting> _settings =
        Options.Create(new ServerSetting { MaxLoginAttempt = 5, DomainName = "https://example.com" });

    private static readonly EmailTemplate _emailTemplate = new() { Subject = "subject", Title = "title", Content0 = "c0", Content1 = "c1" };

    private AccountService CreateSut() => new(
        _accountRepo.Object,
        _passwordService.Object,
        _mailClient.Object,
        _emailPublisher.Object,
        _settings,
        _rsa.Object,
        NullLogger<AccountService>.Instance,
        _unitOfWork.Object);

    // -- Helpers --------------------------------------------------------------

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

    private void SetupMailSuccess()
    {
        _mailClient.Setup(m => m.GetMailConfirmationBody(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("email-body");
        _mailClient.Setup(m => m.GetMailResetPasswordBody(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("email-body");
        _emailPublisher.Setup(p => p.PublishAsync(It.IsAny<SendEmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private void SetupMailFailure()
    {
        _mailClient.Setup(m => m.GetMailConfirmationBody(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("email-body");
        _mailClient.Setup(m => m.GetMailResetPasswordBody(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("email-body");
        _emailPublisher.Setup(p => p.PublishAsync(It.IsAny<SendEmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("RabbitMQ unreachable"));
    }

    // -- LoginAsync -----------------------------------------------------------

    /// <summary>Verifies that <c>LoginAsync</c> returns fail when account not found.</summary>
    [Fact]
    public async Task LoginAsync_ReturnsFail_WhenAccountNotFound()
    {
        _accountRepo.Setup(r => r.FindByEmailForUpdateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Account?)null);
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
        _accountRepo.Setup(r => r.FindByEmailForUpdateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(account);
        // The account-state message is only disclosed once the password proves account ownership.
        _passwordService.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
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
        _accountRepo.Setup(r => r.FindByEmailForUpdateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _passwordService.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        _accountRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        LoginResult result = await sut.LoginAsync(account.Email.Value, "wrong", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("wrong", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Deleted", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>LoginAsync</c> returns fail when account is locked.</summary>
    [Fact]
    public async Task LoginAsync_ReturnsFail_WhenAccountIsLocked()
    {
        var account = ActiveAccount(locked: true);
        _accountRepo.Setup(r => r.FindByEmailForUpdateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(account);
        // The account-state message is only disclosed once the password proves account ownership.
        _passwordService.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        var sut = CreateSut();

        LoginResult result = await sut.LoginAsync(account.Email.Value, "correct", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("Locked", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
        // The password is now verified for every account regardless of state, so a wrong guess
        // cannot be used to probe whether the account exists or is locked (see B3).
        _passwordService.Verify(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    /// <summary>Verifies that <c>LoginAsync</c> returns a failure result and persists the incremented counter when the password is wrong.</summary>
    [Fact]
    public async Task LoginAsync_ReturnsFail_AndPersistsIncrementedCounter_WhenPasswordIsWrong()
    {
        var account = ActiveAccount(loginAttempt: 1);
        _accountRepo.Setup(r => r.FindByEmailForUpdateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _passwordService.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        _accountRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        LoginResult result = await sut.LoginAsync(account.Email.Value, "wrong", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("wrong", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
        // The row-locked account is mutated in memory and persisted within the same transaction,
        // not via a separate atomic UPDATE.
        _accountRepo.Verify(r => r.UpdateEntityAsync(
            It.Is<Account>(a => a.LoginAttempt == 2 && !a.Locked), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Verifies that <c>LoginAsync</c> locks the account when the failed attempt reaches max login attempts.</summary>
    [Fact]
    public async Task LoginAsync_LocksAccount_WhenMaxLoginAttemptsReached()
    {
        var account = ActiveAccount(loginAttempt: _settings.Value.MaxLoginAttempt - 1);
        _accountRepo.Setup(r => r.FindByEmailForUpdateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _passwordService.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        _accountRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        LoginResult result = await sut.LoginAsync(account.Email.Value, "wrong", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        // The wrong-password response is always the generic error — it must not reveal that this
        // failed attempt just locked the account (that would enumerate valid emails). The account
        // is still locked in persisted state; the user learns that on their next attempt.
        Assert.Contains("wrong", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
        _accountRepo.Verify(r => r.UpdateEntityAsync(
            It.Is<Account>(a => a.Locked && a.LoginAttempt == _settings.Value.MaxLoginAttempt), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Verifies that <c>LoginAsync</c> returns fail when email not confirmed.</summary>
    [Fact]
    public async Task LoginAsync_ReturnsFail_WhenEmailNotConfirmed()
    {
        var account = ActiveAccount(emailConfirmed: false);
        _accountRepo.Setup(r => r.FindByEmailForUpdateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _passwordService.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _accountRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
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
        _accountRepo.Setup(r => r.FindByEmailForUpdateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _passwordService.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _accountRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
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
        _accountRepo.Setup(r => r.FindByEmailForUpdateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _passwordService.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _accountRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        LoginResult result = await sut.LoginAsync(account.Email.Value, "correct", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.NotNull(result.Account);
        Assert.Equal(account.Email.Value, result.Account.Email);
        // The session-bound AccountResponse no longer has a HashedPassword member at all — the
        // secret-bearing fields live only on AdminAccountResponse (see AccountMapper). The absence
        // of a password hash here is now a compile-time guarantee, not a runtime assertion.
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // -- RegisterAsync --------------------------------------------------------

    /// <summary>Verifies that <c>RegisterAsync</c> creates account and sends email when email does not exist.</summary>
    [Fact]
    public async Task RegisterAsync_CreatesAccountAndSendsEmail_WhenEmailDoesNotExist()
    {
        _accountRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Account?)null);
        _passwordService.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("$2a$13$hashed");
        _accountRepo.Setup(r => r.CreateAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _rsa.Setup(r => r.Encrypt(It.IsAny<string>())).Returns("enc-token");
        SetupMailSuccess();
        var sut = CreateSut();

        var request = new AccountRequest
        {
            Email = "new@example.com",
            PlainPassword = "PlainPass1!",
            Nickname = "TestUser",
            RegistrationToken = GuidToken.Generate()
        };

        RegisterResult result = await sut.RegisterAsync(request, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.True(result.ShowResendEmail);
        _accountRepo.Verify(r => r.CreateAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Verifies that <c>RegisterAsync</c> stamps the published email with the ambient
    /// <see cref="Activity.Current"/> id, so Worker-side logs for that message can be traced
    /// back to this request.</summary>
    [Fact]
    public async Task RegisterAsync_PublishesEmail_WithCurrentActivityId()
    {
        _accountRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Account?)null);
        _passwordService.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("$2a$13$hashed");
        _accountRepo.Setup(r => r.CreateAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _rsa.Setup(r => r.Encrypt(It.IsAny<string>())).Returns("enc-token");
        SetupMailSuccess();
        using var activity = new Activity("test-request").Start();
        var sut = CreateSut();

        var request = new AccountRequest
        {
            Email = "new@example.com",
            PlainPassword = "PlainPass1!",
            Nickname = "TestUser",
            RegistrationToken = GuidToken.Generate()
        };

        await sut.RegisterAsync(request, _emailTemplate, TestContext.Current.CancellationToken);

        _emailPublisher.Verify(p => p.PublishAsync(
            It.Is<SendEmailMessage>(m => m.CorrelationId == activity.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Verifies that <c>RegisterAsync</c> returns fail when email sending fails.</summary>
    [Fact]
    public async Task RegisterAsync_ReturnsFail_WhenEmailSendingFails()
    {
        _accountRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Account?)null);
        _passwordService.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("$2a$13$hashed");
        _accountRepo.Setup(r => r.CreateAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _accountRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _rsa.Setup(r => r.Encrypt(It.IsAny<string>())).Returns("enc-token");
        SetupMailFailure();
        var sut = CreateSut();

        var request = new AccountRequest
        {
            Email = "new@example.com",
            PlainPassword = "PlainPass1!",
            RegistrationToken = GuidToken.Generate()
        };

        RegisterResult result = await sut.RegisterAsync(request, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>RegisterAsync</c> returns fail when nickname already exists.</summary>
    [Fact]
    public async Task RegisterAsync_ReturnsFail_WhenNicknameAlreadyExists()
    {
        _accountRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Account?)null);
        _passwordService.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("$2a$13$hashed");
        // Simulate a unique constraint violation (Npgsql error code 23505) on the Nickname constraint specifically.
        _accountRepo.Setup(r => r.CreateAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("duplicate key", new Exception("23505 unique constraint \"Account_Nickname_unique\"")));
        var sut = CreateSut();

        var request = new AccountRequest
        {
            Email = "new@example.com",
            PlainPassword = "PlainPass1!",
            Nickname = "TakenNick",
            RegistrationToken = GuidToken.Generate()
        };

        RegisterResult result = await sut.RegisterAsync(request, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("TakenNick", result.ErrorKey);
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
        _accountRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Account?)null);
        _passwordService.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("$2a$13$hashed");
        // Simulate a unique constraint violation on the Email primary key, not the Nickname constraint.
        _accountRepo.Setup(r => r.CreateAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("duplicate key", new Exception("23505 unique constraint \"Account_pk\"")));
        var sut = CreateSut();

        var request = new AccountRequest
        {
            Email = "raced@example.com",
            PlainPassword = "PlainPass1!",
            Nickname = "SomeNick",
            RegistrationToken = GuidToken.Generate()
        };

        RegisterResult result = await sut.RegisterAsync(request, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("raced@example.com", result.ErrorKey);
        Assert.DoesNotContain("Nickname", result.ErrorKey);
    }

    /// <summary>Verifies that <c>RegisterAsync</c> resends confirmation email when account exists but not confirmed.</summary>
    [Fact]
    public async Task RegisterAsync_ResendsConfirmationEmail_WhenAccountExistsButNotConfirmed()
    {
        string token = GuidToken.Generate();
        var existing = ActiveAccount(emailConfirmed: false, registrationToken: token);
        _accountRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _accountRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _rsa.Setup(r => r.Encrypt(It.IsAny<string>())).Returns("enc-token");
        SetupMailSuccess();
        var sut = CreateSut();

        var request = new AccountRequest { Email = existing.Email.Value, HashedPassword = "pw" };
        RegisterResult result = await sut.RegisterAsync(request, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.True(result.ShowResendEmail);
    }

    /// <summary>Verifies that <c>RegisterAsync</c> returns fail when account already confirmed.</summary>
    [Fact]
    public async Task RegisterAsync_ReturnsFail_WhenAccountAlreadyConfirmed()
    {
        var existing = ActiveAccount(emailConfirmed: true);
        _accountRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _accountRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        var request = new AccountRequest { Email = existing.Email.Value, HashedPassword = "pw" };
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
        _accountRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _passwordService.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        var sut = CreateSut();

        var request = new AccountRequest { Email = existing.Email.Value, PlainPassword = "not-the-password" };
        RegisterResult result = await sut.RegisterAsync(request, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("already", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
        _accountRepo.Verify(r => r.UpdateAgreedServiceTermsAsync(
            It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
        _accountRepo.Verify(r => r.UpdateMessageAsync(
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
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
        _accountRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _accountRepo.Setup(r => r.UpdateAgreedServiceTermsAsync(
            It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _accountRepo.Setup(r => r.UpdateMessageAsync(
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _passwordService.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        var sut = CreateSut();

        var request = new AccountRequest { Email = existing.Email.Value, PlainPassword = "correct" };
        RegisterResult result = await sut.RegisterAsync(request, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        _accountRepo.Verify(r => r.UpdateAgreedServiceTermsAsync(
            existing.Email.Value, true, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
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
        _accountRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        var sut = CreateSut();

        RegisterResult result = await sut.ResendConfirmationEmailAsync(existing.Email.Value, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("already", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
        _accountRepo.Verify(r => r.UpdateMessageAsync(
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // -- ConfirmEmailAsync ----------------------------------------------------

    /// <summary>Verifies that <c>ConfirmEmailAsync</c> returns invalid token when decryption fails.</summary>
    [Fact]
    public async Task ConfirmEmailAsync_ReturnsInvalidToken_WhenDecryptionFails()
    {
        _rsa.Setup(r => r.Decrypt(It.IsAny<string>())).Throws(new Exception("bad cipher"));
        var sut = CreateSut();

        ConfirmEmailResult result = await sut.ConfirmEmailAsync("bad-encrypted-token", TestContext.Current.CancellationToken);

        Assert.True(result.InvalidToken);
        Assert.False(result.AccountCreated);
    }

    /// <summary>Verifies that <c>ConfirmEmailAsync</c> returns invalid token when account not found.</summary>
    [Fact]
    public async Task ConfirmEmailAsync_ReturnsInvalidToken_WhenAccountNotFound()
    {
        string freshToken = GuidToken.Generate();
        _rsa.Setup(r => r.Decrypt("enc")).Returns(freshToken);
        _accountRepo.Setup(r => r.FindByRegistrationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Account?)null);
        var sut = CreateSut();

        ConfirmEmailResult result = await sut.ConfirmEmailAsync("enc", TestContext.Current.CancellationToken);

        Assert.True(result.InvalidToken);
        Assert.False(result.AccountCreated);
    }

    /// <summary>Verifies that <c>ConfirmEmailAsync</c> returns invalid token when token is expired.</summary>
    [Fact]
    public async Task ConfirmEmailAsync_ReturnsInvalidToken_WhenTokenIsExpired()
    {
        string expiredToken = BuildExpiredToken();
        _rsa.Setup(r => r.Decrypt("enc-expired")).Returns(expiredToken);
        _accountRepo.Setup(r => r.FindByRegistrationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActiveAccount());
        var sut = CreateSut();

        ConfirmEmailResult result = await sut.ConfirmEmailAsync("enc-expired", TestContext.Current.CancellationToken);

        Assert.True(result.InvalidToken);
        Assert.False(result.AccountCreated);
    }

    /// <summary>Verifies that <c>ConfirmEmailAsync</c> confirms account when token is valid.</summary>
    [Fact]
    public async Task ConfirmEmailAsync_ConfirmsAccount_WhenTokenIsValid()
    {
        string freshToken = GuidToken.Generate();
        var account = ActiveAccount(emailConfirmed: false, registrationToken: freshToken);
        _rsa.Setup(r => r.Decrypt("enc")).Returns(freshToken);
        _accountRepo.Setup(r => r.FindByRegistrationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _accountRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ConfirmEmailResult result = await sut.ConfirmEmailAsync("enc", TestContext.Current.CancellationToken);

        Assert.False(result.InvalidToken);
        Assert.True(result.AccountCreated);
        _accountRepo.Verify(r => r.UpdateEntityAsync(
            It.Is<Account>(a => a.EmailConfirmed && a.RegistrationToken == null), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Verifies that <c>ConfirmEmailAsync</c> returns not created when account already confirmed.</summary>
    [Fact]
    public async Task ConfirmEmailAsync_ReturnsNotCreated_WhenAccountAlreadyConfirmed()
    {
        string freshToken = GuidToken.Generate();
        var account = ActiveAccount(emailConfirmed: true);
        _rsa.Setup(r => r.Decrypt("enc")).Returns(freshToken);
        _accountRepo.Setup(r => r.FindByRegistrationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _accountRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ConfirmEmailResult result = await sut.ConfirmEmailAsync("enc", TestContext.Current.CancellationToken);

        Assert.False(result.InvalidToken);
        Assert.False(result.AccountCreated);
    }

    // -- ForgotPasswordAsync --------------------------------------------------

    /// <summary>Verifies that <c>ForgotPasswordAsync</c> returns true silently when account not found.</summary>
    [Fact]
    public async Task ForgotPasswordAsync_ReturnsTrueSilently_WhenAccountNotFound()
    {
        _accountRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Account?)null);
        var sut = CreateSut();

        bool result = await sut.ForgotPasswordAsync("ghost@example.com", _emailTemplate, TestContext.Current.CancellationToken);

        Assert.True(result);
        _emailPublisher.Verify(p => p.PublishAsync(It.IsAny<SendEmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Verifies that <c>ForgotPasswordAsync</c> returns true silently when account email not confirmed.</summary>
    [Fact]
    public async Task ForgotPasswordAsync_ReturnsTrueSilently_WhenAccountEmailNotConfirmed()
    {
        var account = ActiveAccount(emailConfirmed: false);
        _accountRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(account);
        var sut = CreateSut();

        bool result = await sut.ForgotPasswordAsync(account.Email.Value, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.True(result);
        _emailPublisher.Verify(p => p.PublishAsync(It.IsAny<SendEmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Verifies that <c>ForgotPasswordAsync</c> sends reset email when account is valid.</summary>
    [Fact]
    public async Task ForgotPasswordAsync_SendsResetEmail_WhenAccountIsValid()
    {
        var account = ActiveAccount();
        _accountRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _accountRepo.Setup(r => r.UpdateResetPasswordTokenAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _accountRepo.Setup(r => r.UpdateMessageAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _rsa.Setup(r => r.Encrypt(It.IsAny<string>())).Returns("enc-reset-token");
        SetupMailSuccess();
        var sut = CreateSut();

        bool result = await sut.ForgotPasswordAsync(account.Email.Value, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.True(result);
        // Column-scoped writes (not a full-row UpdateEntityAsync): the reset token first, then the
        // "mail sent" status message.
        _accountRepo.Verify(r => r.UpdateResetPasswordTokenAsync(
            account.Email.Value, It.Is<string?>(t => t != null), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        _accountRepo.Verify(r => r.UpdateMessageAsync(
            account.Email.Value, It.IsAny<string?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        _accountRepo.Verify(r => r.UpdateEntityAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Verifies that <c>ForgotPasswordAsync</c> still sends a reset email when the account is locked,
    /// since resetting the password is the intended way to recover from a login lockout.</summary>
    [Fact]
    public async Task ForgotPasswordAsync_SendsResetEmail_WhenAccountIsLocked()
    {
        var account = ActiveAccount(locked: true);
        _accountRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _accountRepo.Setup(r => r.UpdateResetPasswordTokenAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _accountRepo.Setup(r => r.UpdateMessageAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _rsa.Setup(r => r.Encrypt(It.IsAny<string>())).Returns("enc-reset-token");
        SetupMailSuccess();
        var sut = CreateSut();

        bool result = await sut.ForgotPasswordAsync(account.Email.Value, _emailTemplate, TestContext.Current.CancellationToken);

        Assert.True(result);
        _accountRepo.Verify(r => r.UpdateResetPasswordTokenAsync(
            account.Email.Value, It.Is<string?>(t => t != null), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        _accountRepo.Verify(r => r.UpdateMessageAsync(
            account.Email.Value, It.IsAny<string?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        _accountRepo.Verify(r => r.UpdateEntityAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // -- ValidateResetPasswordTokenAsync -------------------------------------

    /// <summary>Verifies that <c>ValidateResetPasswordTokenAsync</c> returns fail to reset when decryption fails.</summary>
    [Fact]
    public async Task ValidateResetPasswordTokenAsync_ReturnsFailToReset_WhenDecryptionFails()
    {
        _rsa.Setup(r => r.Decrypt(It.IsAny<string>())).Throws(new Exception("bad cipher"));
        var sut = CreateSut();

        ResetPasswordValidationResult result = await sut.ValidateResetPasswordTokenAsync("bad", TestContext.Current.CancellationToken);

        Assert.True(result.FailToReset);
    }

    /// <summary>Verifies that <c>ValidateResetPasswordTokenAsync</c> returns fail to reset when token expired.</summary>
    [Fact]
    public async Task ValidateResetPasswordTokenAsync_ReturnsFailToReset_WhenTokenExpired()
    {
        string expiredToken = BuildExpiredToken();
        _rsa.Setup(r => r.Decrypt("enc")).Returns(expiredToken);
        _accountRepo.Setup(r => r.FindByResetPasswordTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActiveAccount());
        var sut = CreateSut();

        ResetPasswordValidationResult result = await sut.ValidateResetPasswordTokenAsync("enc", TestContext.Current.CancellationToken);

        Assert.True(result.FailToReset);
    }

    /// <summary>Verifies that <c>ValidateResetPasswordTokenAsync</c> returns token when valid.</summary>
    [Fact]
    public async Task ValidateResetPasswordTokenAsync_ReturnsToken_WhenValid()
    {
        string freshToken = GuidToken.Generate();
        _rsa.Setup(r => r.Decrypt("enc")).Returns(freshToken);
        _accountRepo.Setup(r => r.FindByResetPasswordTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActiveAccount());
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
        _rsa.Setup(r => r.Decrypt(It.IsAny<string>())).Throws(new Exception("bad cipher"));
        var sut = CreateSut();

        ServiceResult result = await sut.ResetPasswordAsync("bad", "newpass", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>ResetPasswordAsync</c> returns fail when token expired.</summary>
    [Fact]
    public async Task ResetPasswordAsync_ReturnsFail_WhenTokenExpired()
    {
        string expiredToken = BuildExpiredToken();
        _rsa.Setup(r => r.Decrypt("enc")).Returns(expiredToken);
        _accountRepo.Setup(r => r.FindByResetPasswordTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActiveAccount());
        var sut = CreateSut();

        ServiceResult result = await sut.ResetPasswordAsync("enc", "newpass", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>ResetPasswordAsync</c> updates password and unlocks account when valid.</summary>
    [Fact]
    public async Task ResetPasswordAsync_UpdatesPasswordAndUnlocksAccount_WhenValid()
    {
        string freshToken = GuidToken.Generate();
        var account = ActiveAccount(locked: true, loginAttempt: 3, resetPasswordToken: freshToken);
        _rsa.Setup(r => r.Decrypt("enc")).Returns(freshToken);
        _accountRepo.Setup(r => r.FindByResetPasswordTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _accountRepo.Setup(r => r.FindByEmailForUpdateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _passwordService.Setup(p => p.HashPassword("NewPass1!")).Returns("$2a$13$newhash");
        _accountRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.ResetPasswordAsync("enc", "NewPass1!", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        _accountRepo.Verify(r => r.UpdateEntityAsync(It.Is<Account>(a =>
            a.HashedPassword == "$2a$13$newhash" &&
            a.Locked == false &&
            a.LoginAttempt == 0 &&
            a.ResetPasswordToken == null), It.IsAny<CancellationToken>()), Times.Once);
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
        _accountRepo.Setup(r => r.FindByNicknamesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await CreateSut().GetAccountsByNicknamesAsync(nicknames, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        _accountRepo.Verify(r => r.FindByNicknamesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // -- Private helpers ------------------------------------------------------

    private static string BuildExpiredToken()
    {
        byte[] time = BitConverter.GetBytes(DateTime.UtcNow.AddHours(-25).ToBinary());
        byte[] key = Guid.NewGuid().ToByteArray();
        return Convert.ToBase64String(time.Concat(key).ToArray());
    }
}
