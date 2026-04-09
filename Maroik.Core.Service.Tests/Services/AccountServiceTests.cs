using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Utilities;
using Maroik.Core.Service.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Maroik.Core.Service.Tests.Services;

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

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static AccountResponse ActiveAccount(string email = "user@example.com") => new()
    {
        Email = email,
        HashedPassword = "$2a$13$placeholder",
        EmailConfirmed = true,
        AgreedServiceTerms = true,
        Deleted = false,
        Locked = false,
        LoginAttempt = 0
    };

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

    // ── LoginAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_ReturnsFail_WhenAccountNotFound()
    {
        _accountRepo.Setup(r => r.GetAccountByEmailAsync("missing@example.com")).ReturnsAsync((AccountResponse?)null);
        var sut = CreateSut();

        LoginResult result = await sut.LoginAsync("missing@example.com", "any");

        Assert.False(result.Success);
        Assert.Contains("wrong", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginAsync_ReturnsFail_WhenAccountIsDeleted()
    {
        var account = ActiveAccount();
        account.Deleted = true;
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(account.Email!)).ReturnsAsync(account);
        var sut = CreateSut();

        LoginResult result = await sut.LoginAsync(account.Email!, "any");

        Assert.False(result.Success);
        Assert.Contains("Deleted", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginAsync_ReturnsFail_WhenAccountIsLocked()
    {
        var account = ActiveAccount();
        account.Locked = true;
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(account.Email!)).ReturnsAsync(account);
        var sut = CreateSut();

        LoginResult result = await sut.LoginAsync(account.Email!, "any");

        Assert.False(result.Success);
        Assert.Contains("Locked", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginAsync_ReturnsFail_AndIncrementsAttemptCount_WhenPasswordIsWrong()
    {
        var account = ActiveAccount();
        account.LoginAttempt = 1;
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(account.Email!)).ReturnsAsync(account);
        _passwordService.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<AccountRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        LoginResult result = await sut.LoginAsync(account.Email!, "wrong");

        Assert.False(result.Success);
        // Verify UpdateAccountAsync was called (attempt count incremented)
        _accountRepo.Verify(r => r.UpdateAccountAsync(It.Is<AccountRequest>(req => req.LoginAttempt == 2)), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_LocksAccount_WhenMaxLoginAttemptsReached()
    {
        var account = ActiveAccount();
        account.LoginAttempt = _settings.Value.MaxLoginAttempt - 1;
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(account.Email!)).ReturnsAsync(account);
        _passwordService.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<AccountRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        LoginResult result = await sut.LoginAsync(account.Email!, "wrong");

        Assert.False(result.Success);
        _accountRepo.Verify(r => r.UpdateAccountAsync(It.Is<AccountRequest>(req => req.Locked == true)), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ReturnsFail_WhenEmailNotConfirmed()
    {
        var account = ActiveAccount();
        account.EmailConfirmed = false;
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(account.Email!)).ReturnsAsync(account);
        _passwordService.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<AccountRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        LoginResult result = await sut.LoginAsync(account.Email!, "correct");

        Assert.False(result.Success);
        Assert.Contains("Email verification", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginAsync_ReturnsFail_WhenServiceTermsNotAgreed()
    {
        var account = ActiveAccount();
        account.AgreedServiceTerms = false;
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(account.Email!)).ReturnsAsync(account);
        _passwordService.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<AccountRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        LoginResult result = await sut.LoginAsync(account.Email!, "correct");

        Assert.False(result.Success);
        Assert.Contains("Service Terms", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginAsync_ReturnsSuccessWithAccount_AndClearsHashedPassword_WhenCredentialsValid()
    {
        var account = ActiveAccount();
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(account.Email!)).ReturnsAsync(account);
        _passwordService.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<AccountRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        LoginResult result = await sut.LoginAsync(account.Email!, "correct");

        Assert.True(result.Success);
        Assert.NotNull(result.Account);
        Assert.Equal(account.Email, result.Account.Email);
        Assert.Null(result.Account.HashedPassword); // must be cleared
    }

    // ── RegisterAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_CreatesAccountAndSendsEmail_WhenEmailDoesNotExist()
    {
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(It.IsAny<string>())).ReturnsAsync((AccountResponse?)null);
        _passwordService.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("$2a$13$hashed");
        _accountRepo.Setup(r => r.CreateAccountAsync(It.IsAny<AccountRequest>())).Returns(Task.CompletedTask);
        _rsa.Setup(r => r.Encrypt(It.IsAny<string>())).Returns("enc-token");
        SetupMailSuccess();
        var sut = CreateSut();

        var request = new AccountRequest
        {
            Email = "new@example.com",
            HashedPassword = "plain-password",
            RegistrationToken = GuidToken.Generate()
        };

        RegisterResult result = await sut.RegisterAsync(request, "subject", "title", "c0", "c1");

        Assert.True(result.Success);
        Assert.True(result.ShowResendEmail);
        _accountRepo.Verify(r => r.CreateAccountAsync(It.IsAny<AccountRequest>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsFail_WhenEmailSendingFails()
    {
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(It.IsAny<string>())).ReturnsAsync((AccountResponse?)null);
        _passwordService.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("$2a$13$hashed");
        _accountRepo.Setup(r => r.CreateAccountAsync(It.IsAny<AccountRequest>())).Returns(Task.CompletedTask);
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<AccountRequest>())).Returns(Task.CompletedTask);
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

    [Fact]
    public async Task RegisterAsync_ReturnsFail_WhenNicknameAlreadyExists()
    {
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(It.IsAny<string>())).ReturnsAsync((AccountResponse?)null);
        _passwordService.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("$2a$13$hashed");
        // Simulate unique constraint violation (Npgsql error code 23505)
        _accountRepo.Setup(r => r.CreateAccountAsync(It.IsAny<AccountRequest>()))
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

    [Fact]
    public async Task RegisterAsync_ResendsConfirmationEmail_WhenAccountExistsButNotConfirmed()
    {
        var existing = ActiveAccount();
        existing.EmailConfirmed = false;
        existing.RegistrationToken = GuidToken.Generate();
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(existing.Email!)).ReturnsAsync(existing);
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<AccountRequest>())).Returns(Task.CompletedTask);
        _rsa.Setup(r => r.Encrypt(It.IsAny<string>())).Returns("enc-token");
        SetupMailSuccess();
        var sut = CreateSut();

        var request = new AccountRequest { Email = existing.Email, HashedPassword = "pw" };
        RegisterResult result = await sut.RegisterAsync(request, "subject", "title", "c0", "c1");

        Assert.True(result.Success);
        Assert.True(result.ShowResendEmail);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsFail_WhenAccountAlreadyConfirmed()
    {
        var existing = ActiveAccount();
        existing.EmailConfirmed = true;
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(existing.Email!)).ReturnsAsync(existing);
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<AccountRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        var request = new AccountRequest { Email = existing.Email, HashedPassword = "pw" };
        RegisterResult result = await sut.RegisterAsync(request, "subject", "title", "c0", "c1");

        Assert.False(result.Success);
        Assert.Contains("already", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    // ── ConfirmEmailAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmEmailAsync_ReturnsInvalidToken_WhenDecryptionFails()
    {
        _rsa.Setup(r => r.Decrypt(It.IsAny<string>())).Throws(new Exception("bad cipher"));
        var sut = CreateSut();

        ConfirmEmailResult result = await sut.ConfirmEmailAsync("bad-encrypted-token");

        Assert.True(result.InvalidToken);
        Assert.False(result.AccountCreated);
    }

    [Fact]
    public async Task ConfirmEmailAsync_ReturnsInvalidToken_WhenAccountNotFound()
    {
        string freshToken = GuidToken.Generate();
        _rsa.Setup(r => r.Decrypt("enc")).Returns(freshToken);
        _accountRepo.Setup(r => r.GetAccountByRegistrationTokenAsync(freshToken)).ReturnsAsync((AccountResponse?)null);
        var sut = CreateSut();

        ConfirmEmailResult result = await sut.ConfirmEmailAsync("enc");

        Assert.True(result.InvalidToken);
        Assert.False(result.AccountCreated);
    }

    [Fact]
    public async Task ConfirmEmailAsync_ReturnsInvalidToken_WhenTokenIsExpired()
    {
        string expiredToken = BuildExpiredToken();
        _rsa.Setup(r => r.Decrypt("enc-expired")).Returns(expiredToken);
        _accountRepo.Setup(r => r.GetAccountByRegistrationTokenAsync(expiredToken))
            .ReturnsAsync(ActiveAccount());
        var sut = CreateSut();

        ConfirmEmailResult result = await sut.ConfirmEmailAsync("enc-expired");

        Assert.True(result.InvalidToken);
        Assert.False(result.AccountCreated);
    }

    [Fact]
    public async Task ConfirmEmailAsync_ConfirmsAccount_WhenTokenIsValid()
    {
        string freshToken = GuidToken.Generate();
        var account = ActiveAccount();
        account.EmailConfirmed = false;
        account.RegistrationToken = freshToken;
        _rsa.Setup(r => r.Decrypt("enc")).Returns(freshToken);
        _accountRepo.Setup(r => r.GetAccountByRegistrationTokenAsync(freshToken)).ReturnsAsync(account);
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<AccountRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ConfirmEmailResult result = await sut.ConfirmEmailAsync("enc");

        Assert.False(result.InvalidToken);
        Assert.True(result.AccountCreated);
        _accountRepo.Verify(r => r.UpdateAccountAsync(
            It.Is<AccountRequest>(req => req.EmailConfirmed && req.RegistrationToken == null)), Times.Once);
    }

    [Fact]
    public async Task ConfirmEmailAsync_ReturnsNotCreated_WhenAccountAlreadyConfirmed()
    {
        string freshToken = GuidToken.Generate();
        var account = ActiveAccount();
        account.EmailConfirmed = true; // already confirmed
        _rsa.Setup(r => r.Decrypt("enc")).Returns(freshToken);
        _accountRepo.Setup(r => r.GetAccountByRegistrationTokenAsync(freshToken)).ReturnsAsync(account);
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<AccountRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ConfirmEmailResult result = await sut.ConfirmEmailAsync("enc");

        Assert.False(result.InvalidToken);
        Assert.False(result.AccountCreated);
    }

    // ── ForgotPasswordAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task ForgotPasswordAsync_ReturnsTrueSilently_WhenAccountNotFound()
    {
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(It.IsAny<string>())).ReturnsAsync((AccountResponse?)null);
        var sut = CreateSut();

        bool result = await sut.ForgotPasswordAsync("ghost@example.com", "s", "t", "c0", "c1");

        Assert.True(result);
        _mailClient.Verify(m => m.SendMailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ForgotPasswordAsync_ReturnsTrueSilently_WhenAccountEmailNotConfirmed()
    {
        var account = ActiveAccount();
        account.EmailConfirmed = false;
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(account.Email!)).ReturnsAsync(account);
        var sut = CreateSut();

        bool result = await sut.ForgotPasswordAsync(account.Email!, "s", "t", "c0", "c1");

        Assert.True(result);
        _mailClient.Verify(m => m.SendMailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ForgotPasswordAsync_SendsResetEmail_WhenAccountIsValid()
    {
        var account = ActiveAccount();
        _accountRepo.Setup(r => r.GetAccountByEmailAsync(account.Email!)).ReturnsAsync(account);
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<AccountRequest>())).Returns(Task.CompletedTask);
        _rsa.Setup(r => r.Encrypt(It.IsAny<string>())).Returns("enc-reset-token");
        SetupMailSuccess();
        var sut = CreateSut();

        bool result = await sut.ForgotPasswordAsync(account.Email!, "s", "t", "c0", "c1");

        Assert.True(result);
        _accountRepo.Verify(r => r.UpdateAccountAsync(
            It.Is<AccountRequest>(req => req.ResetPasswordToken != null)), Times.Once);
    }

    // ── ValidateResetPasswordTokenAsync ─────────────────────────────────────

    [Fact]
    public async Task ValidateResetPasswordTokenAsync_ReturnsFailToReset_WhenDecryptionFails()
    {
        _rsa.Setup(r => r.Decrypt(It.IsAny<string>())).Throws(new Exception("bad cipher"));
        var sut = CreateSut();

        ResetPasswordValidationResult result = await sut.ValidateResetPasswordTokenAsync("bad");

        Assert.True(result.FailToReset);
    }

    [Fact]
    public async Task ValidateResetPasswordTokenAsync_ReturnsFailToReset_WhenTokenExpired()
    {
        string expiredToken = BuildExpiredToken();
        _rsa.Setup(r => r.Decrypt("enc")).Returns(expiredToken);
        _accountRepo.Setup(r => r.GetAccountByResetPasswordTokenAsync(expiredToken))
            .ReturnsAsync(ActiveAccount());
        var sut = CreateSut();

        ResetPasswordValidationResult result = await sut.ValidateResetPasswordTokenAsync("enc");

        Assert.True(result.FailToReset);
    }

    [Fact]
    public async Task ValidateResetPasswordTokenAsync_ReturnsToken_WhenValid()
    {
        string freshToken = GuidToken.Generate();
        _rsa.Setup(r => r.Decrypt("enc")).Returns(freshToken);
        _accountRepo.Setup(r => r.GetAccountByResetPasswordTokenAsync(freshToken))
            .ReturnsAsync(ActiveAccount());
        var sut = CreateSut();

        ResetPasswordValidationResult result = await sut.ValidateResetPasswordTokenAsync("enc");

        Assert.False(result.FailToReset);
        Assert.Equal("enc", result.ResetPasswordToken);
    }

    // ── ResetPasswordAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task ResetPasswordAsync_ReturnsFail_WhenDecryptionFails()
    {
        _rsa.Setup(r => r.Decrypt(It.IsAny<string>())).Throws(new Exception("bad cipher"));
        var sut = CreateSut();

        ServiceResult result = await sut.ResetPasswordAsync("bad", "newpass");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ResetPasswordAsync_ReturnsFail_WhenTokenExpired()
    {
        string expiredToken = BuildExpiredToken();
        _rsa.Setup(r => r.Decrypt("enc")).Returns(expiredToken);
        _accountRepo.Setup(r => r.GetAccountByResetPasswordTokenAsync(expiredToken))
            .ReturnsAsync(ActiveAccount());
        var sut = CreateSut();

        ServiceResult result = await sut.ResetPasswordAsync("enc", "newpass");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ResetPasswordAsync_UpdatesPasswordAndUnlocksAccount_WhenValid()
    {
        string freshToken = GuidToken.Generate();
        var account = ActiveAccount();
        account.Locked = true;
        account.LoginAttempt = 3;
        _rsa.Setup(r => r.Decrypt("enc")).Returns(freshToken);
        _accountRepo.Setup(r => r.GetAccountByResetPasswordTokenAsync(freshToken)).ReturnsAsync(account);
        _passwordService.Setup(p => p.HashPassword("newpass")).Returns("$2a$13$newhash");
        _accountRepo.Setup(r => r.UpdateAccountAsync(It.IsAny<AccountRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.ResetPasswordAsync("enc", "newpass");

        Assert.True(result.Success);
        _accountRepo.Verify(r => r.UpdateAccountAsync(It.Is<AccountRequest>(req =>
            req.HashedPassword == "$2a$13$newhash" &&
            req.Locked == false &&
            req.LoginAttempt == 0 &&
            req.ResetPasswordToken == null)), Times.Once);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static string BuildExpiredToken()
    {
        byte[] time = BitConverter.GetBytes(DateTime.UtcNow.AddHours(-25).ToBinary());
        byte[] key = Guid.NewGuid().ToByteArray();
        return Convert.ToBase64String(time.Concat(key).ToArray());
    }
}
