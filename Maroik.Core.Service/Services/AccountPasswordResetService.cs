using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Enums;
using Maroik.Core.Contract.Extensions;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Settings;
using Maroik.Core.Domain.Accounts;
using Maroik.Core.Domain.ValueObjects;
using Maroik.Core.Service.Extensions;
using Maroik.Core.Service.Mappers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Maroik.Core.Service.Services;

/// <summary>
/// Implementation of <see cref="IAccountPasswordResetService"/>: the forgotten-password flow. The reset token is generated
/// by <see cref="GuidToken"/> and protected by Data Protection (<see cref="IDataProtectionService"/>) in the mailed link.
/// Email bodies are built via <see cref="IMailBodyComposer"/> and handed off to <see cref="IEmailPublisher"/>, which enqueues them
/// for asynchronous SMTP delivery by Maroik.Worker instead of sending inline.
/// </summary>
public class AccountPasswordResetService(
    IAccountRepository accountRepository,
    IPasswordService passwordService,
    IMailBodyComposer mailBodies,
    IEmailPublisher emailPublisher,
    IOptions<ServerSetting> settings,
    IDataProtectionService dataProtection,
    ILogger<AccountPasswordResetService> logger,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider
    ) : IAccountPasswordResetService
{
    /// <summary>The token inside a reset link's protected value, or <see langword="null"/> (logged) when the value does not decrypt.</summary>
    private string? TryUnprotectResetPasswordToken(string encryptedToken)
    {
        try { return dataProtection.Unprotect(ProtectedValue.MailToken, encryptedToken); }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to decrypt reset password token");
            return null;
        }
    }

    /// <summary>An address no account can have (the primary key is never empty): the target of the timing-equalization write.</summary>
    private const string NoAccountEmail = "";

    /// <inheritdoc />
    public async Task<bool> ForgotPasswordAsync(string email, EmailTemplate emailTemplate, CancellationToken ct = default)
    {
        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;
        email = Email.NormalizeForLookup(email);
        Account? account = await accountRepository.FindByEmailAsync(email, ct);

        // Silently succeed even when the email is unknown / unconfirmed to avoid user enumeration.
        // The response body is already identical, but the success path below spends a token
        // generation, a Data Protection protect and two column writes that an early `return true` would skip —
        // a measurable timing oracle for "is this a registered, confirmed address?". Spend the
        // equivalent work here first, mirroring LoginAsync's VerifyDummy + dummy-write hardening.
        // (The async mail publish on the success path is the only residual asymmetry, the same
        // trade-off LoginAsync accepts.)
        if (account is not { CanRequestPasswordReset: true })
        {
            try
            {
                _ = dataProtection.Protect(ProtectedValue.MailToken, GuidToken.Generate(utcNow));
                // The write targets NoAccountEmail, never `email`: for an address that IS registered
                // (but unconfirmed or deleted) this branch must not touch that stranger's row — an
                // anonymous caller who merely knows the address could otherwise rewrite its Updated
                // stamp and clear its token. An empty address matches no row (Account.Email is the
                // non-empty primary key), so the statement costs the same and changes nothing.
                _ = await accountRepository.UpdateResetPasswordTokenAsync(NoAccountEmail, null, utcNow, ct);
            }
            catch (Exception e) { logger.LogWarning(e, "ForgotPassword equalization failed for {Email}", email); }
            logger.LogWarning("Password reset ignored for {Email}: unknown, unconfirmed or deleted account", email);
            return true;
        }

        string resetPasswordToken = GuidToken.Generate(utcNow);

        try
        {
            // Persist the reset token FIRST, so any mail that goes out corresponds to a token the
            // database will actually accept. The reverse order can e-mail a link whose token was
            // never saved (mail sent, then the save fails) — a dead link the user cannot use.
            // Cannot fail: RequestPasswordReset only errors when EmailConfirmed is false, already ruled out above.
            _ = account.RequestPasswordReset(resetPasswordToken, utcNow);

            // Column-scoped writes (ResetPasswordToken/Message + Updated), not a full-row
            // UpdateEntityAsync: this runs off an un-locked read, so a concurrent self-service
            // profile change (avatar/timezone/password) on the same account must not be reverted by
            // a stale full-row snapshot — same rationale as every other Account write path.
            await accountRepository.UpdateResetPasswordTokenAsync(
                account.Email.Value, account.ResetPasswordToken, account.Updated, ct);

            string body = mailBodies.GetMailResetPasswordBody(dataProtection.Protect(ProtectedValue.MailToken, resetPasswordToken), emailTemplate.Title, emailTemplate.Content0, emailTemplate.Content1, settings.Value.DomainName ?? "");
            bool published = await emailPublisher.TryPublishAsync(logger, account.Email.Value, emailTemplate.Subject, body, ct);
            if (published)
                logger.LogInformation("Password reset requested for {Email}", account.Email.Value);

            account.SetMessage((published ? AccountMessage.ResetPasswordMail : AccountMessage.FailToMailSent).GetDescription(), utcNow);
            await accountRepository.UpdateMessageAsync(account.Email.Value, account.Message, account.Updated, ct);
        }
        catch (Exception e) { logger.LogWarning(e, "ForgotPassword failed for {Email}", email); }

        return true;
    }

    /// <inheritdoc />
    public async Task<ResetPasswordValidationResult> ValidateResetPasswordTokenAsync(string encryptedToken, CancellationToken ct = default)
    {
        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;
        if (TryUnprotectResetPasswordToken(encryptedToken) is not { } rawToken)
            return new ResetPasswordValidationResult { FailToReset = true };

        Account? account = await FindByResetPasswordTokenAsync(rawToken, ct);

        // Reject if account not found, deleted, token has expired, or email is unconfirmed. Mirrors
        // the same guard on ForgotPasswordAsync / ResetPasswordAsync so a soft-deleted account's
        // lingering token can never be reported "valid" here only to be rejected at the actual reset.
        // A valid link returns the still-encrypted token so the client can pass it back during the actual reset.
        if (account != null && account.AcceptsResetToken(rawToken, utcNow))
            return new ResetPasswordValidationResult
            {
                FailToReset = false, ResetPasswordToken = encryptedToken
            };
        logger.LogWarning("Password reset link rejected: invalid or expired token");
        return new ResetPasswordValidationResult { FailToReset = true };
    }

    /// <inheritdoc />
    public async Task<(ServiceResult Result, AccountResponse? SignIn)> ResetPasswordAsync(string encryptedToken, string newPassword, CancellationToken ct = default)
    {
        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;
        if (TryUnprotectResetPasswordToken(encryptedToken) is not { } rawToken)
            return (ServiceResult.Fail(ServiceErrorKeys.Signals.ResetPasswordInvalid), null);

        Account? found = await FindByResetPasswordTokenAsync(rawToken, ct);
        if (found == null || !found.AcceptsResetToken(rawToken, utcNow))
        {
            logger.LogWarning("Password reset rejected: invalid or expired token");
            return (ServiceResult.Fail(ServiceErrorKeys.Signals.ResetPasswordInvalid), null);
        }

        // Enforce the complexity rule server-side, before hashing — Account.ResetPassword only sees the hash.
        if (!PasswordPolicy.IsValid(newPassword))
            return (ServiceResult.Validation("Account.PasswordPolicy", PasswordPolicy.ViolationMessage), null);

        string newHashedPassword = passwordService.HashPassword(newPassword);

        await unitOfWork.BeginAsync(ct: ct);
        try
        {
            // The lookup above was by token and unlocked. Re-read the row under FOR UPDATE before
            // writing: this touches HashedPassword / SecurityStamp / Locked, so it must not run as a
            // stale full-row overwrite that reverts a concurrent self-service change on the same
            // account (the user may be signed in on another device).
            Account? account = await accountRepository.FindByEmailForUpdateAsync(found.Email.Value, ct);
            if (account == null || account.Deleted)
            {
                await unitOfWork.RollbackAsync(ct);
                logger.LogWarning("Password reset rejected: account {Email} no longer exists or is deleted", found.Email.Value);
                return (ServiceResult.Fail(ServiceErrorKeys.Signals.ResetPasswordInvalid), null);
            }

            // ResetPassword re-checks the stored token, so a concurrent reset that already consumed
            // it between the lookup and this lock is rejected here (compare-and-swap).
            var resetResult = account.ResetPassword(rawToken, newHashedPassword, utcNow);
            if (resetResult.IsError)
            {
                await unitOfWork.RollbackAsync(ct);
                logger.LogWarning("Password reset rejected: invalid or expired token for {Email}", account.Email.Value);
                return (ServiceResult.Fail(ServiceErrorKeys.Signals.ResetPasswordInvalid), null);
            }

            account.SetMessage(AccountMessage.SuccessToResetPassword.GetDescription(), utcNow);

            await accountRepository.UpdateEntityAsync(account, ct);
            await unitOfWork.CommitAsync(ct);
            logger.LogInformation("Password reset completed for {Email}", account.Email.Value);

            // The holder of the mailed link has just proven the mailbox and chosen the password, so they may be
            // signed in straight away — nothing a password guesser could use (the lockout guards guessing; this
            // path needs the single-use token). The account handed back carries the NEW security stamp. Not
            // deleted and email-confirmed are already guaranteed above; the remaining LoginAsync refusal
            // (service terms not accepted) still applies, so a reset is no way around it.
            return (ServiceResult.Ok(), account.CheckSignInOnceAuthenticated() is null ? AccountMapper.ToResponse(account) : null);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to reset password for {Email}", found.Email.Value);
            await unitOfWork.RollbackAsync(ct);
            return (ServiceResult.Failure("Account.ResetPasswordFailed", ServiceErrorKeys.ResetPasswordFailed), null);
        }
    }

    /// <summary>
    /// Unlocked lookup of the account holding <paramref name="token"/> (raw, already decrypted) as its password-reset token,
    /// searched for by its <see cref="GuidToken.Hash"/> as the row stores it.
    /// </summary>
    private Task<Account?> FindByResetPasswordTokenAsync(string token, CancellationToken ct = default)
        => accountRepository.FindByResetPasswordTokenAsync(GuidToken.Hash(token), ct);
}
