using Maroik.Core.Contract.Constants;
using Maroik.Core.Contract.Domains.Account;
using Maroik.Core.Contract.Enums;
using Maroik.Core.Contract.Helpers;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Messaging;
using Maroik.Core.Contract.Settings;
using Maroik.Worker.Handlers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Maroik.Worker.Tests.Handlers;

/// <summary>
/// Unit tests for <see cref="EmailMessageHandler"/>.
/// <see cref="IMailClient"/> and <see cref="IAccountRepository"/> are mocked so no real
/// SMTP server or database is needed.
/// </summary>
public class EmailMessageHandlerTests
{
    private readonly Mock<IMailClient> _mailClient = new();
    private readonly Mock<IAccountRepository> _accountRepository = new();
    private readonly IOptions<ServerSetting> _settings = Options.Create(new ServerSetting
    {
        SmtpHost = "smtp.example.com",
        SmtpPort = 587,
        SmtpSsl = true,
        SmtpUserName = "user",
        SmtpPassword = "pass",
        FromEmail = "noreply@maroik.com",
        FromFullName = "Maroik"
    });

    private EmailMessageHandler CreateSut() => new(
        _mailClient.Object,
        _accountRepository.Object,
        _settings,
        NullLogger<EmailMessageHandler>.Instance);

    private static Account ActiveAccount(string email = "user@example.com") => Account.Reconstitute(
        email: email,
        hashedPassword: "$2a$13$placeholder",
        nickname: "User",
        avatarImagePath: null,
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

    /// <summary>Verifies that <c>HandleAsync</c> does not touch the repository when the SMTP send succeeds.</summary>
    [Fact]
    public async Task HandleAsync_DoesNotUpdateAccount_WhenMailSentSuccessfully()
    {
        _mailClient.Setup(m => m.SendMailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EnumHelper.GetDescription(AccountMessage.MailSent));

        var sut = CreateSut();
        var message = new SendEmailMessage("user@example.com", "subject", "body");

        await sut.HandleAsync(message, TestContext.Current.CancellationToken);

        _accountRepository.Verify(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _accountRepository.Verify(r => r.UpdateEntityAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Verifies that <c>HandleAsync</c> marks the account as FailToMailSent when SMTP fails and the account exists.</summary>
    [Fact]
    public async Task HandleAsync_MarksAccountFailToMailSent_WhenSmtpFailsAndAccountFound()
    {
        var account = ActiveAccount();
        _mailClient.Setup(m => m.SendMailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("SMTP connection refused");
        _accountRepository.Setup(r => r.FindByEmailAsync(account.Email.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _accountRepository.Setup(r => r.UpdateEntityAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var message = new SendEmailMessage(account.Email.Value, "subject", "body");

        await sut.HandleAsync(message, TestContext.Current.CancellationToken);

        _accountRepository.Verify(r => r.UpdateEntityAsync(
            It.Is<Account>(a => a.Message == EnumHelper.GetDescription(AccountMessage.FailToMailSent)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Verifies that <c>HandleAsync</c> does not throw when SMTP fails and no matching account exists.</summary>
    [Fact]
    public async Task HandleAsync_DoesNotThrow_WhenSmtpFailsAndAccountNotFound()
    {
        _mailClient.Setup(m => m.SendMailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("SMTP connection refused");
        _accountRepository.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var sut = CreateSut();
        var message = new SendEmailMessage("ghost@example.com", "subject", "body");

        await sut.HandleAsync(message, TestContext.Current.CancellationToken);

        _accountRepository.Verify(r => r.UpdateEntityAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
