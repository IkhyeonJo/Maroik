using Maroik.Core.Contract.Utilities;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Mapster;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Maroik.Core.Service.Services;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordService _passwordService;
    private readonly IMailClient _mailClient;
    private readonly IRsaService _rsa;
    private readonly IOptions<ServerSetting> _settings;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        IAccountRepository accountRepository,
        IPasswordService passwordService,
        IMailClient mailClient,
        IOptions<ServerSetting> settings,
        IRsaService rsa,
        ILogger<AccountService> logger)
    {
        _accountRepository = accountRepository;
        _passwordService = passwordService;
        _mailClient = mailClient;
        _settings = settings;
        _rsa = rsa;
        _logger = logger;
    }

    public async Task<List<AccountResponse>> GetAllAccountsAsync()
        => await _accountRepository.GetAllAsync();

    public Task<AccountResponse?> GetAccountByEmailAsync(string email)
        => _accountRepository.GetAccountByEmailAsync(email);

    public async Task<LoginResult> LoginAsync(string email, string password)
    {
        AccountResponse? account = await _accountRepository.GetAccountByEmailAsync(email);

        if (account == null)
            return LoginResult.Fail("Email or Password is wrong");

        if (account.Deleted)
            return LoginResult.Fail("Your Account is Deleted, Please create your new account by clicking Sign up Button");

        if (account.Locked)
            return LoginResult.Fail("Your Account is Locked, Please reset your password by clicking Forgot password Button");

        if (!_passwordService.VerifyPassword(password, account.HashedPassword ?? ""))
        {
            account.LoginAttempt++;
            if (account.LoginAttempt == _settings.Value.MaxLoginAttempt)
            {
                account.Locked = true;
                account.Message = EnumHelper.GetDescription(AccountMessage.AccountLocked);
                await _accountRepository.UpdateAccountAsync(account.Adapt<AccountRequest>());
                return LoginResult.Fail("Your Account is Locked, Please reset your password by clicking Forgot password Button");
            }
            await _accountRepository.UpdateAccountAsync(account.Adapt<AccountRequest>());
            return LoginResult.Fail("Email or Password is wrong");
        }

        if (!account.EmailConfirmed)
            return LoginResult.Fail("Email verification was not completed. Please try sign up again.");

        if (!account.AgreedServiceTerms)
            return LoginResult.Fail("Agreed Service Terms was not checked. Please try sign up again and login again.");

        account.LoginAttempt = 0;
        await _accountRepository.UpdateAccountAsync(account.Adapt<AccountRequest>());
        account.HashedPassword = null;
        return LoginResult.Ok(account);
    }

    public async Task<RegisterResult> RegisterAsync(AccountRequest newAccount, string emailSubject, string emailTitle, string emailContent0, string emailContent1)
    {
        AccountResponse? existing = await _accountRepository.GetAccountByEmailAsync(newAccount.Email ?? "");

        if (existing == null)
        {
            newAccount.HashedPassword = _passwordService.HashPassword(newAccount.HashedPassword ?? "");
            try
            {
                await _accountRepository.CreateAccountAsync(newAccount);
            }
            catch (Exception e)
            {
                if (IsUniqueViolation(e))
                    return RegisterResult.Fail($"'{newAccount.Nickname}' is a Nickname that already exists. Please enter another Nickname.");
                _logger.LogError(e, "Failed to create account for {Email}", newAccount.Email);
                return RegisterResult.Fail("Error occurred while processing about account registration");
            }

            // Encrypt raw token so the URL token can be looked up directly in DB on confirmation
            string body = _mailClient.GetMailConfirmationBody(_rsa.Encrypt(newAccount.RegistrationToken!), emailTitle, emailContent0, emailContent1, _settings.Value.DomainName ?? string.Empty);
            string mailResult = await SendMailAsync(newAccount.Email!, emailSubject, body);
            if (mailResult != EnumHelper.GetDescription(AccountMessage.MailSent))
            {
                try
                {
                    newAccount.Message = EnumHelper.GetDescription(AccountMessage.FailToMailSent);
                    await _accountRepository.UpdateAccountAsync(newAccount);
                }
                catch (Exception e) { _logger.LogWarning(e, "Failed to update account message after mail send failure"); }
                return RegisterResult.Fail("Error occurred while processing about sending account authentication mail");
            }

            return RegisterResult.Ok(showResendEmail: true, email: newAccount.Email);
        }

        if (!existing.EmailConfirmed)
        {
            if (string.IsNullOrEmpty(existing.RegistrationToken))
                existing.RegistrationToken = GuidToken.Generate();

            string body = _mailClient.GetMailConfirmationBody(_rsa.Encrypt(existing.RegistrationToken), emailTitle, emailContent0, emailContent1, _settings.Value.DomainName ?? string.Empty);
            string mailResult = await SendMailAsync(existing.Email!, emailSubject, body);

            if (mailResult != EnumHelper.GetDescription(AccountMessage.MailSent))
            {
                try
                {
                    existing.Updated = DateTime.UtcNow;
                    existing.Message = EnumHelper.GetDescription(AccountMessage.FailToMailSent);
                    await _accountRepository.UpdateAccountAsync(existing.Adapt<AccountRequest>());
                }
                catch (Exception e) { _logger.LogWarning(e, "Failed to update account message after mail send failure for {Email}", existing.Email); }
                return RegisterResult.Fail("Error occurred while processing about sending account authentication mail");
            }

            try
            {
                existing.Message = EnumHelper.GetDescription(AccountMessage.VerifyEmail);
                existing.Updated = DateTime.UtcNow;
                await _accountRepository.UpdateAccountAsync(existing.Adapt<AccountRequest>());
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to update account status for {Email}", existing.Email);
                return RegisterResult.Fail("Error occurred while processing about account status");
            }

            return RegisterResult.Ok(showResendEmail: true, email: existing.Email);
        }

        try
        {
            existing.Message = EnumHelper.GetDescription(AccountMessage.UserAlreadyCreated);
            existing.Updated = DateTime.UtcNow;
            existing.AgreedServiceTerms = true;
            await _accountRepository.UpdateAccountAsync(existing.Adapt<AccountRequest>());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to update account status for {Email}", existing.Email);
            return RegisterResult.Fail("Error occurred while processing about account status");
        }

        return RegisterResult.Fail(EnumHelper.GetDescription(AccountMessage.UserAlreadyCreated));
    }

    public async Task<RegisterResult> ResendConfirmationEmailAsync(string email, string emailSubject, string emailTitle, string emailContent0, string emailContent1)
    {
        AccountResponse? account = await _accountRepository.GetAccountByEmailAsync(email);

        if (account == null)
            return RegisterResult.Fail("Failed to resend email", showResendEmail: true);

        if (!account.EmailConfirmed)
        {
            if (string.IsNullOrEmpty(account.RegistrationToken))
                account.RegistrationToken = GuidToken.Generate();

            string body = _mailClient.GetMailConfirmationBody(_rsa.Encrypt(account.RegistrationToken), emailTitle, emailContent0, emailContent1, _settings.Value.DomainName ?? string.Empty);
            string mailResult = await SendMailAsync(email, emailSubject, body);
            if (mailResult != EnumHelper.GetDescription(AccountMessage.MailSent))
            {
                try
                {
                    account.Updated = DateTime.UtcNow;
                    account.Message = EnumHelper.GetDescription(AccountMessage.FailToMailSent);
                    await _accountRepository.UpdateAccountAsync(account.Adapt<AccountRequest>());
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to update account status for {Email}", email);
                    return RegisterResult.Fail("Error occurred while processing about account status");
                }
                return RegisterResult.Fail("Error occurred while processing about sending account authentication mail");
            }

            try
            {
                account.Message = EnumHelper.GetDescription(AccountMessage.VerifyEmail);
                account.Updated = DateTime.UtcNow;
                await _accountRepository.UpdateAccountAsync(account.Adapt<AccountRequest>());
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to update account status for {Email}", email);
                return RegisterResult.Fail("Error occurred while processing about mail send account status");
            }

            return RegisterResult.Ok(showResendEmail: true, email: account.Email, repeat: true);
        }

        try
        {
            account.Message = EnumHelper.GetDescription(AccountMessage.UserAlreadyCreated);
            account.Updated = DateTime.UtcNow;
            await _accountRepository.UpdateAccountAsync(account.Adapt<AccountRequest>());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to update account status for {Email}", email);
            return RegisterResult.Fail("Error occurred while processing about account status");
        }

        return RegisterResult.Fail(EnumHelper.GetDescription(AccountMessage.UserAlreadyCreated));
    }

    public async Task<ConfirmEmailResult> ConfirmEmailAsync(string encryptedToken)
    {
        // Decrypt URL token to get the raw GUID stored in DB — eliminates O(n) account scan
        string rawToken;
        try { rawToken = _rsa.Decrypt(encryptedToken); }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Failed to decrypt registration token");
            return new ConfirmEmailResult { InvalidToken = true, AccountCreated = false };
        }

        AccountResponse? account = await _accountRepository.GetAccountByRegistrationTokenAsync(rawToken);

        if (account == null)
            return new ConfirmEmailResult { InvalidToken = true, AccountCreated = false };

        if (!GuidToken.IsTokenAlive(rawToken))
            return new ConfirmEmailResult { InvalidToken = true, AccountCreated = false };

        if (!account.EmailConfirmed)
        {
            account.RegistrationToken = null;
            account.EmailConfirmed = true;
            account.Updated = DateTime.UtcNow;
            account.Message = EnumHelper.GetDescription(AccountMessage.Success);
            try
            {
                await _accountRepository.UpdateAccountAsync(account.Adapt<AccountRequest>());
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to confirm email for account {Email}", account.Email);
                return new ConfirmEmailResult
                {
                    InvalidToken = true,
                    AccountCreated = true,
                    ErrorKey = "Error occurred while processing about confirm email account status"
                };
            }
            return new ConfirmEmailResult { InvalidToken = false, AccountCreated = true };
        }

        account.Updated = DateTime.UtcNow;
        account.Message = EnumHelper.GetDescription(AccountMessage.UserAlreadyCreated);
        try
        {
            await _accountRepository.UpdateAccountAsync(account.Adapt<AccountRequest>());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to update account status for {Email}", account.Email);
            return new ConfirmEmailResult
            {
                InvalidToken = true,
                AccountCreated = true,
                ErrorKey = "Error occurred while processing about account status"
            };
        }
        return new ConfirmEmailResult { InvalidToken = false, AccountCreated = false };
    }

    public async Task<bool> ForgotPasswordAsync(string email, string emailSubject, string emailTitle, string emailContent0, string emailContent1)
    {
        AccountResponse? account = await _accountRepository.GetAccountByEmailAsync(email);

        if (account == null || !account.EmailConfirmed)
            return true; // silently succeed (security: don't reveal if email exists)

        string resetPasswordToken = GuidToken.Generate();
        try
        {
            // Encrypt raw token for URL; DB stores raw for direct lookup
            string body = _mailClient.GetMailResetPasswordBody(_rsa.Encrypt(resetPasswordToken), emailTitle, emailContent0, emailContent1, _settings.Value.DomainName ?? string.Empty);

            string mailResult = await SendMailAsync(account.Email!, emailSubject, body);
            if (mailResult != EnumHelper.GetDescription(AccountMessage.MailSent))
            {
                account.Updated = DateTime.UtcNow;
                account.Message = EnumHelper.GetDescription(AccountMessage.FailToMailSent);
                await _accountRepository.UpdateAccountAsync(account.Adapt<AccountRequest>());
                return true;
            }

            account.ResetPasswordToken = resetPasswordToken;
            account.Updated = DateTime.UtcNow;
            account.Message = EnumHelper.GetDescription(AccountMessage.ResetPasswordMail);
            await _accountRepository.UpdateAccountAsync(account.Adapt<AccountRequest>());
        }
        catch (Exception e) { _logger.LogWarning(e, "ForgotPassword failed for {Email}", email); }

        return true;
    }

    public async Task<ResetPasswordValidationResult> ValidateResetPasswordTokenAsync(string encryptedToken)
    {
        string rawToken;
        try { rawToken = _rsa.Decrypt(encryptedToken); }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Failed to decrypt reset password token");
            return new ResetPasswordValidationResult { FailToReset = true };
        }

        AccountResponse? account = await _accountRepository.GetAccountByResetPasswordTokenAsync(rawToken);

        if (account == null || !GuidToken.IsTokenAlive(rawToken) || !account.EmailConfirmed)
            return new ResetPasswordValidationResult { FailToReset = true };

        return new ResetPasswordValidationResult { FailToReset = false, ResetPasswordToken = encryptedToken };
    }

    public async Task<ServiceResult> ResetPasswordAsync(string encryptedToken, string newPassword)
    {
        string rawToken;
        try { rawToken = _rsa.Decrypt(encryptedToken); }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Failed to decrypt reset password token");
            return ServiceResult.Fail("reset-password-invalid");
        }

        AccountResponse? account = await _accountRepository.GetAccountByResetPasswordTokenAsync(rawToken);

        if (account == null || !GuidToken.IsTokenAlive(rawToken) || !account.EmailConfirmed)
            return ServiceResult.Fail("reset-password-invalid");

        account.ResetPasswordToken = null;
        account.HashedPassword = _passwordService.HashPassword(newPassword);
        account.Locked = false;
        account.LoginAttempt = 0;
        account.Updated = DateTime.UtcNow;
        account.Message = EnumHelper.GetDescription(AccountMessage.SuccessToResetPassword);
        try
        {
            await _accountRepository.UpdateAccountAsync(account.Adapt<AccountRequest>());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to reset password for {Email}", account.Email);
            return ServiceResult.Fail("Error occurred while processing about reset password");
        }

        return ServiceResult.Ok();
    }

    private Task<string> SendMailAsync(string to, string subject, string body)
        => _mailClient.SendMailAsync(
            to, subject, body,
            _settings.Value.SmtpHost ?? string.Empty,
            _settings.Value.SmtpPort.ToString(),
            _settings.Value.SmtpUserName ?? string.Empty,
            _settings.Value.SmtpPassword ?? string.Empty,
            _settings.Value.SmtpSsl.ToString(),
            _settings.Value.FromEmail ?? string.Empty,
            _settings.Value.FromFullName ?? string.Empty);

    private static bool IsUniqueViolation(Exception e)
        => e.InnerException?.Message?.Contains("23505") == true
           || (e.InnerException?.GetType().Name.Contains("NpgsqlException") == true
               && e.InnerException.Message.Contains("unique", StringComparison.OrdinalIgnoreCase));
}
