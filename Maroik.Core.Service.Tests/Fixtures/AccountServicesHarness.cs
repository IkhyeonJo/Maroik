using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Settings;
using Maroik.Core.Service.Services;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

namespace Maroik.Core.Service.Tests.Fixtures;

/// <summary>
/// The four account services over one set of collaborators and one log collector, for the test classes whose account flows
/// cross them (a login after a registration's state, the audit trail of them all). Each member forwards to its service.
/// </summary>
/// <param name="accountRepository">The account repository they share.</param>
/// <param name="passwordService">The password service they share.</param>
/// <param name="mailBodies">The mail client of the flows that send a mail.</param>
/// <param name="emailPublisher">The e-mail queue of the flows that send a mail.</param>
/// <param name="settings">The server settings they share.</param>
/// <param name="dataProtection">The protector of the mailed tokens.</param>
/// <param name="logs">Collects what every service logs.</param>
/// <param name="unitOfWork">The unit of work they share.</param>
/// <param name="timeProvider">The clock they share.</param>
internal sealed class AccountServicesHarness(
    IAccountRepository accountRepository,
    IPasswordService passwordService,
    IMailBodyComposer mailBodies,
    IEmailPublisher emailPublisher,
    IOptions<ServerSetting> settings,
    IDataProtectionService dataProtection,
    FakeLogCollector logs,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    /// <summary>The reads of accounts.</summary>
    private readonly AccountService _reads = new(accountRepository);

    /// <summary>The login flow.</summary>
    private readonly AccountLoginService _login = new(
        accountRepository, passwordService, settings, new FakeLogger<AccountLoginService>(logs), unitOfWork, timeProvider);

    /// <summary>The registration and confirmation flows.</summary>
    private readonly AccountRegistrationService _registration = new(
        accountRepository, passwordService, mailBodies, emailPublisher, settings, dataProtection,
        new FakeLogger<AccountRegistrationService>(logs), unitOfWork, timeProvider);

    /// <summary>The forgotten-password flow.</summary>
    private readonly AccountPasswordResetService _passwordReset = new(
        accountRepository, passwordService, mailBodies, emailPublisher, settings, dataProtection,
        new FakeLogger<AccountPasswordResetService>(logs), unitOfWork, timeProvider);

    /// <inheritdoc cref="AccountService.GetAccountsByNicknamesAsync"/>
    public Task<List<AccountResponse>> GetAccountsByNicknamesAsync(IEnumerable<string> nicknames, CancellationToken ct = default) =>
        _reads.GetAccountsByNicknamesAsync(nicknames, ct);

    /// <inheritdoc cref="AccountService.GetAccountByEmailAsync"/>
    public Task<AccountResponse?> GetAccountByEmailAsync(string email, CancellationToken ct = default) =>
        _reads.GetAccountByEmailAsync(email, ct);

    /// <inheritdoc cref="AccountLoginService.LoginAsync"/>
    public Task<LoginResult> LoginAsync(string email, string password, CancellationToken ct = default) =>
        _login.LoginAsync(email, password, ct);

    /// <inheritdoc cref="AccountRegistrationService.RegisterAsync"/>
    public Task<RegisterResult> RegisterAsync(RegisterAccountRequest newAccount, EmailTemplate emailTemplate, CancellationToken ct = default) =>
        _registration.RegisterAsync(newAccount, emailTemplate, ct);

    /// <inheritdoc cref="AccountRegistrationService.ResendConfirmationEmailAsync"/>
    public Task<RegisterResult> ResendConfirmationEmailAsync(string email, EmailTemplate emailTemplate, CancellationToken ct = default) =>
        _registration.ResendConfirmationEmailAsync(email, emailTemplate, ct);

    /// <inheritdoc cref="AccountRegistrationService.ValidateRegistrationTokenAsync"/>
    public Task<ConfirmEmailResult> ValidateRegistrationTokenAsync(string encryptedToken, CancellationToken ct = default) =>
        _registration.ValidateRegistrationTokenAsync(encryptedToken, ct);

    /// <inheritdoc cref="AccountRegistrationService.ConfirmEmailAsync"/>
    public Task<ConfirmEmailResult> ConfirmEmailAsync(string encryptedToken, string password, CancellationToken ct = default) =>
        _registration.ConfirmEmailAsync(encryptedToken, password, ct);

    /// <inheritdoc cref="AccountPasswordResetService.ForgotPasswordAsync"/>
    public Task<bool> ForgotPasswordAsync(string email, EmailTemplate emailTemplate, CancellationToken ct = default) =>
        _passwordReset.ForgotPasswordAsync(email, emailTemplate, ct);

    /// <inheritdoc cref="AccountPasswordResetService.ValidateResetPasswordTokenAsync"/>
    public Task<ResetPasswordValidationResult> ValidateResetPasswordTokenAsync(string encryptedToken, CancellationToken ct = default) =>
        _passwordReset.ValidateResetPasswordTokenAsync(encryptedToken, ct);

    /// <inheritdoc cref="AccountPasswordResetService.ResetPasswordAsync"/>
    public Task<(ServiceResult Result, AccountResponse? SignIn)> ResetPasswordAsync(string encryptedToken, string newPassword, CancellationToken ct = default) =>
        _passwordReset.ResetPasswordAsync(encryptedToken, newPassword, ct);
}
