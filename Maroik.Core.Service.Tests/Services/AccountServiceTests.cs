using Maroik.Core.Contract.Domains.Account;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Filters;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Utilities;
using Maroik.Core.Service.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// Unit tests for <see cref="AccountService"/>.
/// All external dependencies (repository, password service, mail client, RSA service)
/// are replaced with Moq mocks so tests run without a database or SMTP server.
/// Covers login, registration, email confirmation, and password-reset flows.
/// </summary>
public class AccountServiceTests
{
    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly Mock<IPasswordService> _passwordService = new();
    private readonly Mock<IMailClient> _mailClient = new();
    private readonly Mock<IRsaService> _rsa = new();
    private readonly IOptions<ServerSetting> _settings =
        Options.Create(new ServerSetting { MaxLoginAttempt = 5, DomainName = "https://example.com" });

    private AccountService CreateSut() => new(
        _accountRepo.Object,
        _passwordService.Object,
        _mailClient.Object,
        _settings,
        _rsa.Object,
        NullLogger<AccountService>.Instance);

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
            deleted: deleted);

    private void SetupMailSuccess()
    {
        _mailClient.Setup(m => m.GetMailConfirmationBody(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("email-body");
        _mailClient.Setup(m => m.GetMailResetPasswordBody(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("email-body");
        _mailClient.Setup(m => m.SendMailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(EnumHelper.GetDescription(AccountMessage.MailSent));
    }

    private void SetupMailFailure()
    {
        _mailClient.Setup(m => m.GetMailConfirmationBody(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("email-body");
        _mailClient.Setup(m => m.GetMailResetPasswordBody(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("email-body");
        _mailClient.Setup(m => m.SendMailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("SMTP error");
    }

    // -- LoginAsync -----------------------------------------------------------

    /// <summary>Verifies that <c>LoginAsync</c> returns fail when account not found.</summary>
    [Fact]
    public async Task LoginAsync_ReturnsFail_WhenAccountNotFound()
    {
        _accountRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync([]);
        var sut = CreateSut();

        LoginResult result = await sut.LoginAsync("missing@example.com", "any");

        Assert.False(result.Success);
        Assert.Contains("wrong", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>LoginAsync</c> returns fail when account is deleted.</summary>
    [Fact]
    public async Task LoginAsync_ReturnsFail_WhenAccountIsDeleted()
    {
        var account = ActiveAccount(deleted: true);
        _accountRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync([account]);
        var sut = CreateSut();

        LoginResult result = await sut.LoginAsync(account.Email.Value, "any");

        Assert.False(result.Success);
        Assert.Contains("Deleted", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>LoginAsync</c> returns fail when account is locked.</summary>
    [Fact]
    public async Task LoginAsync_ReturnsFail_WhenAccountIsLocked()
    {
        var account = ActiveAccount(locked: true);
        _accountRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync([account]);
        var sut = CreateSut();

        LoginResult result = await sut.LoginAsync(account.Email.Value, "any");

        Assert.False(result.Success);
        Assert.Contains("Locked", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>LoginAsync</c> returns a failure result and increments the login attempt count when the password is wrong.</summary>
    [Fact]
    public async Task LoginAsync_ReturnsFail_AndIncrementsAttemptCount_WhenPasswordIsWrong()
    {
        var account = ActiveAccount(loginAttempt: 1);
        _accountRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync([account]);
        _passwordService.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        _accountRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        LoginResult result = await sut.LoginAsync(account.Email.Value, "wrong");

        Assert.False(result.Success);
        // Verify UpdateAccountAsync was called (attempt count incremented)
        _accountRepo.Verify(r => r.UpdateEntityAsync(It.Is<Account>(a => a.LoginAttempt == 2)), Times.Once);
    }

    /// <summary>Verifies that <c>LoginAsync</c> locks account when max login attempts reached.</summary>
    [Fact]
    public async Task LoginAsync_LocksAccount_WhenMaxLoginAttemptsReached()
    {
        var account = ActiveAccount(loginAttempt: _settings.Value.MaxLoginAttempt - 1);
        _accountRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync([account]);
        _passwordService.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        _accountRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        LoginResult result = await sut.LoginAsync(account.Email.Value, "wrong");

        Assert.False(result.Success);
        _accountRepo.Verify(r => r.UpdateEntityAsync(It.Is<Account>(a => a.Locked == true)), Times.Once);
    }

    /// <summary>Verifies that <c>LoginAsync</c> returns fail when email not confirmed.</summary>
    [Fact]
    public async Task LoginAsync_ReturnsFail_WhenEmailNotConfirmed()
    {
        var account = ActiveAccount(emailConfirmed: false);
        _accountRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync([account]);
        _passwordService.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _accountRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        LoginResult result = await sut.LoginAsync(account.Email.Value, "correct");

        Assert.False(result.Success);
        Assert.Contains("Email verification", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>LoginAsync</c> returns fail when service terms not agreed.</summary>
    [Fact]
    public async Task LoginAsync_ReturnsFail_WhenServiceTermsNotAgreed()
    {
        var account = ActiveAccount(agreedServiceTerms: false);
        _accountRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync([account]);
        _passwordService.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _accountRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        LoginResult result = await sut.LoginAsync(account.Email.Value, "correct");

        Assert.False(result.Success);
        Assert.Contains("Service Terms", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>LoginAsync</c> returns a success result with account data and clears the hashed password when credentials are valid.</summary>
    [Fact]
    public async Task LoginAsync_ReturnsSuccessWithAccount_AndClearsHashedPassword_WhenCredentialsValid()
    {
        var account = ActiveAccount();
        _accountRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync([account]);
        _passwordService.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _accountRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        LoginResult result = await sut.LoginAsync(account.Email.Value, "correct");

        Assert.True(result.Success);
        Assert.NotNull(result.Account);
        Assert.Equal(account.Email.Value, result.Account.Email);
        Assert.Null(result.Account.HashedPassword); // must be cleared
    }

    // -- RegisterAsync --------------------------------------------------------

    /// <summary>Verifies that <c>RegisterAsync</c> creates account and sends email when email does not exist.</summary>
    [Fact]
    public async Task RegisterAsync_CreatesAccountAndSendsEmail_WhenEmailDoesNotExist()
    {
        _accountRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync([]);
        _passwordService.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("$2a$13$hashed");
        _accountRepo.Setup(r => r.CreateAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        _rsa.Setup(r => r.Encrypt(It.IsAny<string>())).Returns("enc-token");
        SetupMailSuccess();
        var sut = CreateSut();

        var request = new AccountRequest
        {
            Email = "new@example.com",
            HashedPassword = "plain-password",
            Nickname = "TestUser",
            RegistrationToken = GuidToken.Generate()
        };

        RegisterResult result = await sut.RegisterAsync(request, "subject", "title", "c0", "c1");

        Assert.True(result.Success);
        Assert.True(result.ShowResendEmail);
        _accountRepo.Verify(r => r.CreateAsync(It.IsAny<Account>()), Times.Once);
    }

    /// <summary>Verifies that <c>RegisterAsync</c> returns fail when email sending fails.</summary>
    [Fact]
    public async Task RegisterAsync_ReturnsFail_WhenEmailSendingFails()
    {
        _accountRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync([]);
        _passwordService.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("$2a$13$hashed");
        _accountRepo.Setup(r => r.CreateAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        _accountRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        _rsa.Setup(r => r.Encrypt(It.IsAny<string>())).Returns("enc-token");
        SetupMailFailure();
        var sut = CreateSut();

        var request = new AccountRequest
        {
            Email = "new@example.com",
            HashedPassword = "plain-password",
            RegistrationToken = GuidToken.Generate()
        };

        RegisterResult result = await sut.RegisterAsync(request, "subject", "title", "c0", "c1");

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>RegisterAsync</c> returns fail when nickname already exists.</summary>
    [Fact]
    public async Task RegisterAsync_ReturnsFail_WhenNicknameAlreadyExists()
    {
        _accountRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync([]);
        _passwordService.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("$2a$13$hashed");
        // Simulate unique constraint violation (Npgsql error code 23505)
        _accountRepo.Setup(r => r.CreateAsync(It.IsAny<Account>()))
            .ThrowsAsync(new Exception("duplicate key", new Exception("23505 unique")));
        var sut = CreateSut();

        var request = new AccountRequest
        {
            Email = "new@example.com",
            HashedPassword = "plain-password",
            Nickname = "TakenNick",
            RegistrationToken = GuidToken.Generate()
        };

        RegisterResult result = await sut.RegisterAsync(request, "subject", "title", "c0", "c1");

        Assert.False(result.Success);
        Assert.Contains("TakenNick", result.ErrorKey);
    }

    /// <summary>Verifies that <c>RegisterAsync</c> resends confirmation email when account exists but not confirmed.</summary>
    [Fact]
    public async Task RegisterAsync_ResendsConfirmationEmail_WhenAccountExistsButNotConfirmed()
    {
        string token = GuidToken.Generate();
        var existing = ActiveAccount(emailConfirmed: false, registrationToken: token);
        _accountRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync([existing]);
        _accountRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        _rsa.Setup(r => r.Encrypt(It.IsAny<string>())).Returns("enc-token");
        SetupMailSuccess();
        var sut = CreateSut();

        var request = new AccountRequest { Email = existing.Email.Value, HashedPassword = "pw" };
        RegisterResult result = await sut.RegisterAsync(request, "subject", "title", "c0", "c1");

        Assert.True(result.Success);
        Assert.True(result.ShowResendEmail);
    }

    /// <summary>Verifies that <c>RegisterAsync</c> returns fail when account already confirmed.</summary>
    [Fact]
    public async Task RegisterAsync_ReturnsFail_WhenAccountAlreadyConfirmed()
    {
        var existing = ActiveAccount(emailConfirmed: true);
        _accountRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync([existing]);
        _accountRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        var request = new AccountRequest { Email = existing.Email.Value, HashedPassword = "pw" };
        RegisterResult result = await sut.RegisterAsync(request, "subject", "title", "c0", "c1");

        Assert.False(result.Success);
        Assert.Contains("already", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    // -- ConfirmEmailAsync ----------------------------------------------------

    /// <summary>Verifies that <c>ConfirmEmailAsync</c> returns invalid token when decryption fails.</summary>
    [Fact]
    public async Task ConfirmEmailAsync_ReturnsInvalidToken_WhenDecryptionFails()
    {
        _rsa.Setup(r => r.Decrypt(It.IsAny<string>())).Throws(new Exception("bad cipher"));
        var sut = CreateSut();

        ConfirmEmailResult result = await sut.ConfirmEmailAsync("bad-encrypted-token");

        Assert.True(result.InvalidToken);
        Assert.False(result.AccountCreated);
    }

    /// <summary>Verifies that <c>ConfirmEmailAsync</c> returns invalid token when account not found.</summary>
    [Fact]
    public async Task ConfirmEmailAsync_ReturnsInvalidToken_WhenAccountNotFound()
    {
        string freshToken = GuidToken.Generate();
        _rsa.Setup(r => r.Decrypt("enc")).Returns(freshToken);
        _accountRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync([]);
        var sut = CreateSut();

        ConfirmEmailResult result = await sut.ConfirmEmailAsync("enc");

        Assert.True(result.InvalidToken);
        Assert.False(result.AccountCreated);
    }

    /// <summary>Verifies that <c>ConfirmEmailAsync</c> returns invalid token when token is expired.</summary>
    [Fact]
    public async Task ConfirmEmailAsync_ReturnsInvalidToken_WhenTokenIsExpired()
    {
        string expiredToken = BuildExpiredToken();
        _rsa.Setup(r => r.Decrypt("enc-expired")).Returns(expiredToken);
        _accountRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>()))
            .ReturnsAsync([ActiveAccount()]);
        var sut = CreateSut();

        ConfirmEmailResult result = await sut.ConfirmEmailAsync("enc-expired");

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
        _accountRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync([account]);
        _accountRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ConfirmEmailResult result = await sut.ConfirmEmailAsync("enc");

        Assert.False(result.InvalidToken);
        Assert.True(result.AccountCreated);
        _accountRepo.Verify(r => r.UpdateEntityAsync(
            It.Is<Account>(a => a.EmailConfirmed && a.RegistrationToken == null)), Times.Once);
    }

    /// <summary>Verifies that <c>ConfirmEmailAsync</c> returns not created when account already confirmed.</summary>
    [Fact]
    public async Task ConfirmEmailAsync_ReturnsNotCreated_WhenAccountAlreadyConfirmed()
    {
        string freshToken = GuidToken.Generate();
        var account = ActiveAccount(emailConfirmed: true);
        _rsa.Setup(r => r.Decrypt("enc")).Returns(freshToken);
        _accountRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync([account]);
        _accountRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ConfirmEmailResult result = await sut.ConfirmEmailAsync("enc");

        Assert.False(result.InvalidToken);
        Assert.False(result.AccountCreated);
    }

    // -- ForgotPasswordAsync --------------------------------------------------

    /// <summary>Verifies that <c>ForgotPasswordAsync</c> returns true silently when account not found.</summary>
    [Fact]
    public async Task ForgotPasswordAsync_ReturnsTrueSilently_WhenAccountNotFound()
    {
        _accountRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync([]);
        var sut = CreateSut();

        bool result = await sut.ForgotPasswordAsync("ghost@example.com", "s", "t", "c0", "c1");

        Assert.True(result);
        _mailClient.Verify(m => m.SendMailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>Verifies that <c>ForgotPasswordAsync</c> returns true silently when account email not confirmed.</summary>
    [Fact]
    public async Task ForgotPasswordAsync_ReturnsTrueSilently_WhenAccountEmailNotConfirmed()
    {
        var account = ActiveAccount(emailConfirmed: false);
        _accountRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync([account]);
        var sut = CreateSut();

        bool result = await sut.ForgotPasswordAsync(account.Email.Value, "s", "t", "c0", "c1");

        Assert.True(result);
        _mailClient.Verify(m => m.SendMailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>Verifies that <c>ForgotPasswordAsync</c> sends reset email when account is valid.</summary>
    [Fact]
    public async Task ForgotPasswordAsync_SendsResetEmail_WhenAccountIsValid()
    {
        var account = ActiveAccount();
        _accountRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync([account]);
        _accountRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        _rsa.Setup(r => r.Encrypt(It.IsAny<string>())).Returns("enc-reset-token");
        SetupMailSuccess();
        var sut = CreateSut();

        bool result = await sut.ForgotPasswordAsync(account.Email.Value, "s", "t", "c0", "c1");

        Assert.True(result);
        _accountRepo.Verify(r => r.UpdateEntityAsync(
            It.Is<Account>(a => a.ResetPasswordToken != null)), Times.Once);
    }

    // -- ValidateResetPasswordTokenAsync -------------------------------------

    /// <summary>Verifies that <c>ValidateResetPasswordTokenAsync</c> returns fail to reset when decryption fails.</summary>
    [Fact]
    public async Task ValidateResetPasswordTokenAsync_ReturnsFailToReset_WhenDecryptionFails()
    {
        _rsa.Setup(r => r.Decrypt(It.IsAny<string>())).Throws(new Exception("bad cipher"));
        var sut = CreateSut();

        ResetPasswordValidationResult result = await sut.ValidateResetPasswordTokenAsync("bad");

        Assert.True(result.FailToReset);
    }

    /// <summary>Verifies that <c>ValidateResetPasswordTokenAsync</c> returns fail to reset when token expired.</summary>
    [Fact]
    public async Task ValidateResetPasswordTokenAsync_ReturnsFailToReset_WhenTokenExpired()
    {
        string expiredToken = BuildExpiredToken();
        _rsa.Setup(r => r.Decrypt("enc")).Returns(expiredToken);
        _accountRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>()))
            .ReturnsAsync([ActiveAccount()]);
        var sut = CreateSut();

        ResetPasswordValidationResult result = await sut.ValidateResetPasswordTokenAsync("enc");

        Assert.True(result.FailToReset);
    }

    /// <summary>Verifies that <c>ValidateResetPasswordTokenAsync</c> returns token when valid.</summary>
    [Fact]
    public async Task ValidateResetPasswordTokenAsync_ReturnsToken_WhenValid()
    {
        string freshToken = GuidToken.Generate();
        _rsa.Setup(r => r.Decrypt("enc")).Returns(freshToken);
        _accountRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>()))
            .ReturnsAsync([ActiveAccount()]);
        var sut = CreateSut();

        ResetPasswordValidationResult result = await sut.ValidateResetPasswordTokenAsync("enc");

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

        ServiceResult result = await sut.ResetPasswordAsync("bad", "newpass");

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>ResetPasswordAsync</c> returns fail when token expired.</summary>
    [Fact]
    public async Task ResetPasswordAsync_ReturnsFail_WhenTokenExpired()
    {
        string expiredToken = BuildExpiredToken();
        _rsa.Setup(r => r.Decrypt("enc")).Returns(expiredToken);
        _accountRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>()))
            .ReturnsAsync([ActiveAccount()]);
        var sut = CreateSut();

        ServiceResult result = await sut.ResetPasswordAsync("enc", "newpass");

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>ResetPasswordAsync</c> updates password and unlocks account when valid.</summary>
    [Fact]
    public async Task ResetPasswordAsync_UpdatesPasswordAndUnlocksAccount_WhenValid()
    {
        string freshToken = GuidToken.Generate();
        var account = ActiveAccount(locked: true, loginAttempt: 3, resetPasswordToken: freshToken);
        _rsa.Setup(r => r.Decrypt("enc")).Returns(freshToken);
        _accountRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync([account]);
        _passwordService.Setup(p => p.HashPassword("newpass")).Returns("$2a$13$newhash");
        _accountRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.ResetPasswordAsync("enc", "newpass");

        Assert.True(result.Success);
        _accountRepo.Verify(r => r.UpdateEntityAsync(It.Is<Account>(a =>
            a.HashedPassword == "$2a$13$newhash" &&
            a.Locked == false &&
            a.LoginAttempt == 0 &&
            a.ResetPasswordToken == null)), Times.Once);
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
        _accountRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync(expected);

        var result = await CreateSut().GetAccountsByNicknamesAsync(nicknames);

        Assert.Equal(2, result.Count);
        _accountRepo.Verify(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>()), Times.Once);
    }

    // -- Private helpers ------------------------------------------------------

    private static string BuildExpiredToken()
    {
        byte[] time = BitConverter.GetBytes(DateTime.UtcNow.AddHours(-25).ToBinary());
        byte[] key = Guid.NewGuid().ToByteArray();
        return Convert.ToBase64String(time.Concat(key).ToArray());
    }
}
