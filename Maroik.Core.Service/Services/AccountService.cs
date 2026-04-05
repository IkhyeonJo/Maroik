using Maroik.Core.Contract.Utilities;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Mapster;
using Microsoft.Extensions.Options;

namespace Maroik.Core.Service.Services;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordService _passwordService;
    private readonly IMailClient _mailClient;
    private readonly IRSAService _rsa;
    private readonly IOptions<ServerSetting> _settings;

    public AccountService(IAccountRepository accountRepository, IPasswordService passwordService, IMailClient mailClient, IOptions<ServerSetting> settings, IRSAService rsa)
    {
        _accountRepository = accountRepository;
        _passwordService = passwordService;
        _mailClient = mailClient;
        _settings = settings;
        _rsa = rsa;
    }

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

    public async Task<RegisterResult> RegisterAsync(AccountRequest newAccount, string emailSubject, Func<string, string> emailBodyFactory)
    {
        AccountResponse? existing = await _accountRepository.GetAccountByEmailAsync(newAccount.Email ?? "");

        if (existing == null)
        {
            // New registration — hash password before storing
            newAccount.HashedPassword = _passwordService.HashPassword(newAccount.HashedPassword ?? "");
            try
            {
                await _accountRepository.CreateAccountAsync(newAccount);
            }
            catch (Exception e)
            {
                // PostgreSQL unique constraint violation (SqlState 23505) — nickname already taken
                if (IsUniqueViolation(e))
                    return RegisterResult.Fail($"'{newAccount.Nickname}' is a Nickname that already exists. Please enter another Nickname.");
                return RegisterResult.Fail("Error occurred while processing about account registration");
            }

            string body = emailBodyFactory(_rsa.Sign(newAccount.RegistrationToken!));
            string mailResult = await SendMailAsync(newAccount.Email!, emailSubject, body);
            if (mailResult != EnumHelper.GetDescription(AccountMessage.MailSent))
            {
                try
                {
                    newAccount.Message = EnumHelper.GetDescription(AccountMessage.FailToMailSent);
                    await _accountRepository.UpdateAccountAsync(newAccount);
                }
                catch { /* best effort */ }
                return RegisterResult.Fail("Error occurred while processing about sending account authentication mail");
            }

            return RegisterResult.Ok(showResendEmail: true, email: newAccount.Email);
        }

        // Account already exists but email not confirmed
        if (!existing.EmailConfirmed)
        {
            if (string.IsNullOrEmpty(existing.RegistrationToken))
                existing.RegistrationToken = GUIDToken.Generate();

            string body = emailBodyFactory(_rsa.Sign(existing.RegistrationToken));
            string mailResult = await SendMailAsync(existing.Email!, emailSubject, body);

            if (mailResult != EnumHelper.GetDescription(AccountMessage.MailSent))
            {
                try
                {
                    existing.Updated = DateTime.UtcNow;
                    existing.Message = EnumHelper.GetDescription(AccountMessage.FailToMailSent);
                    await _accountRepository.UpdateAccountAsync(existing.Adapt<AccountRequest>());
                }
                catch { /* best effort */ }
                return RegisterResult.Fail("Error occurred while processing about sending account authentication mail");
            }

            try
            {
                existing.Message = EnumHelper.GetDescription(AccountMessage.VerifyEmail);
                existing.Updated = DateTime.UtcNow;
                await _accountRepository.UpdateAccountAsync(existing.Adapt<AccountRequest>());
            }
            catch
            {
                return RegisterResult.Fail("Error occurred while processing about account status");
            }

            return RegisterResult.Ok(showResendEmail: true, email: existing.Email);
        }

        // Email already confirmed
        try
        {
            existing.Message = EnumHelper.GetDescription(AccountMessage.UserAlreadyCreated);
            existing.Updated = DateTime.UtcNow;
            existing.AgreedServiceTerms = true;
            await _accountRepository.UpdateAccountAsync(existing.Adapt<AccountRequest>());
        }
        catch
        {
            return RegisterResult.Fail("Error occurred while processing about account status");
        }

        return RegisterResult.Fail(EnumHelper.GetDescription(AccountMessage.UserAlreadyCreated));
    }

    public async Task<RegisterResult> ResendConfirmationEmailAsync(string email, string emailSubject, Func<string, string> emailBodyFactory)
    {
        AccountResponse? account = await _accountRepository.GetAccountByEmailAsync(email);

        if (account == null)
            return RegisterResult.Fail("Failed to resend email", showResendEmail: true);

        if (!account.EmailConfirmed)
        {
            if (string.IsNullOrEmpty(account.RegistrationToken))
                account.RegistrationToken = GUIDToken.Generate();

            string body = emailBodyFactory(_rsa.Sign(account.RegistrationToken));
            string mailResult = await SendMailAsync(email, emailSubject, body);
            if (mailResult != EnumHelper.GetDescription(AccountMessage.MailSent))
            {
                try
                {
                    account.Updated = DateTime.UtcNow;
                    account.Message = EnumHelper.GetDescription(AccountMessage.FailToMailSent);
                    await _accountRepository.UpdateAccountAsync(account.Adapt<AccountRequest>());
                }
                catch
                {
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
            catch
            {
                return RegisterResult.Fail("Error occurred while processing about mail send account status");
            }

            return RegisterResult.Ok(showResendEmail: true, email: account.Email, repeat: true);
        }

        // Already confirmed
        try
        {
            account.Message = EnumHelper.GetDescription(AccountMessage.UserAlreadyCreated);
            account.Updated = DateTime.UtcNow;
            await _accountRepository.UpdateAccountAsync(account.Adapt<AccountRequest>());
        }
        catch
        {
            return RegisterResult.Fail("Error occurred while processing about account status");
        }

        return RegisterResult.Fail(EnumHelper.GetDescription(AccountMessage.UserAlreadyCreated));
    }

    public async Task<ConfirmEmailResult> ConfirmEmailAsync(string registrationToken)
    {
        AccountResponse? account = null;

        foreach (AccountResponse temp in await _accountRepository.GetAllAsync())
        {
            try
            {
                if (_rsa.Verify(temp.RegistrationToken ?? string.Empty, registrationToken))
                {
                    account = temp;
                    break;
                }
            }
            catch { /* token mismatch */ }
        }

        if (account == null)
            return new ConfirmEmailResult { InvalidToken = true, AccountCreated = false };

        if (!GUIDToken.IsTokenAlive(account.RegistrationToken!))
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
            catch
            {
                return new ConfirmEmailResult
                {
                    InvalidToken = true,
                    AccountCreated = true,
                    ErrorKey = "Error occurred while processing about confirm email account status"
                };
            }
            return new ConfirmEmailResult { InvalidToken = false, AccountCreated = true };
        }

        // Already confirmed
        account.Updated = DateTime.UtcNow;
        account.Message = EnumHelper.GetDescription(AccountMessage.UserAlreadyCreated);
        try
        {
            await _accountRepository.UpdateAccountAsync(account.Adapt<AccountRequest>());
        }
        catch
        {
            return new ConfirmEmailResult
            {
                InvalidToken = true,
                AccountCreated = true,
                ErrorKey = "Error occurred while processing about account status"
            };
        }
        return new ConfirmEmailResult { InvalidToken = false, AccountCreated = false };
    }

    public async Task<bool> ForgotPasswordAsync(string email, string emailSubject, Func<string, string> emailBodyFactory)
    {
        AccountResponse? account = await _accountRepository.GetAccountByEmailAsync(email);

        if (account == null || !account.EmailConfirmed)
            return true; // silently succeed (security: don't reveal if email exists)

        string resetPasswordToken = GUIDToken.Generate();
        try
        {
            string signedToken = _rsa.Sign(resetPasswordToken);
            string body = emailBodyFactory(signedToken);

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
        catch { /* best effort */ }

        return true;
    }

    public async Task<ResetPasswordValidationResult> ValidateResetPasswordTokenAsync(string resetPasswordToken)
    {
        AccountResponse? account = null;

        foreach (AccountResponse temp in await _accountRepository.GetAllAsync())
        {
            try
            {
                if (_rsa.Verify(temp.ResetPasswordToken ?? string.Empty, resetPasswordToken))
                {
                    account = temp;
                    break;
                }
            }
            catch { /* token mismatch */ }
        }

        if (account == null || !GUIDToken.IsTokenAlive(account.ResetPasswordToken!) || !account.EmailConfirmed)
            return new ResetPasswordValidationResult { FailToReset = true };

        return new ResetPasswordValidationResult { FailToReset = false, ResetPasswordToken = resetPasswordToken };
    }

    public async Task<ServiceResult> ResetPasswordAsync(string resetPasswordToken, string newPassword)
    {
        AccountResponse? account = null;

        foreach (AccountResponse temp in await _accountRepository.GetAllAsync())
        {
            try
            {
                if (_rsa.Verify(temp.ResetPasswordToken ?? string.Empty, resetPasswordToken))
                {
                    account = temp;
                    break;
                }
            }
            catch { /* token mismatch */ }
        }

        if (account == null || !GUIDToken.IsTokenAlive(account.ResetPasswordToken!) || !account.EmailConfirmed)
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
        catch
        {
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
            _settings.Value.SmtpSSL.ToString(),
            _settings.Value.FromEmail ?? string.Empty,
            _settings.Value.FromFullName ?? string.Empty);

    private static bool IsUniqueViolation(Exception e)
        => e.InnerException?.Message?.Contains("23505") == true
           || (e.InnerException?.GetType().Name.Contains("NpgsqlException") == true
               && e.InnerException.Message.Contains("unique", StringComparison.OrdinalIgnoreCase));
}
