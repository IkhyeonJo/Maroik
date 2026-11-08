using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Settings;
using Maroik.Core.Domain.Accounts;
using Maroik.Core.Domain.ValueObjects;
using Maroik.Core.Service.Mappers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Maroik.Core.Service.Services;

/// <summary>
/// Implementation of <see cref="IAccountLoginService"/>. Passwords are verified with BCrypt (<see cref="IPasswordService"/>),
/// and an account is locked after excessive failed attempts (threshold from <see cref="ServerSetting"/>).
/// <para>
/// BCrypt runs on an unlocked read, before the transaction opens: it is deliberately slow, and holding a row lock and a
/// pooled database connection for its whole length would let a burst of logins exhaust the pool. The outcome is then
/// written under a <c>FOR UPDATE</c> re-read, so the failed-attempt counter stays exact and a lock or a password change
/// made during the check still decides. The price is that guesses arriving together are all verified before the first of
/// them sets the lock; each is still counted, and a correct one is still refused once the lock is set.
/// </para>
/// </summary>
public class AccountLoginService(
    IAccountRepository accountRepository,
    IPasswordService passwordService,
    IOptions<ServerSetting> settings,
    ILogger<AccountLoginService> logger,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IAccountLoginService
{
    /// <summary>An address no account can have (the primary key is never empty): the target of the timing-equalization write.</summary>
    private const string NoAccountEmail = "";

    /// <inheritdoc />
    public async Task<LoginResult> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;
        // Matched in the stored form: a mobile autocomplete's trailing space must not fail the login.
        email = Email.NormalizeForLookup(email);

        Account? account = await accountRepository.FindByEmailAsync(email, ct);
        if (account == null)
            return await RefuseUnknownAccountAsync(email, password, utcNow, ct);

        // A locked account is refused BEFORE the password is looked at, whatever was typed. If the
        // password were verified first, the lock would stop nothing: guessing could go on
        // indefinitely, and the one correct guess would be confirmed by the "Locked" reply — a
        // password oracle. Refusing up front ends the guessing (no verification, no counter
        // write, no BCrypt cost) and leaves the lock lifted only by a password reset, a password change from a
        // session still signed in, or an admin.
        // What this reveals is that the address exists and is locked; existence is already public
        // (RegisterAsync answers "already exists"), and anyone can lock an account with a few wrong
        // guesses anyway, so nothing new is exposed.
        if (account.Locked)
            return RefuseLocked(email);

        // For every other state the password is verified FIRST. The remaining account-state
        // messages (deleted / unconfirmed / terms) are only disclosed to a caller that has
        // proven it owns the account, so a wrong password cannot be used to probe what state the
        // account is in — every wrong guess returns the same generic error.
        bool passwordMatches = passwordService.VerifyPassword(password, account.HashedPassword);

        return await RecordOutcomeAsync(email, account.HashedPassword, passwordMatches, utcNow, ct);
    }

    /// <summary>
    /// The answer to an address no account has. It spends the same CPU as a real verification and the same database work as
    /// the wrong-password path (a row lock read, a column write and a commit), so "no such account" and "wrong password" take
    /// the same time — otherwise the response time enumerates valid emails.
    /// </summary>
    private async Task<LoginResult> RefuseUnknownAccountAsync(string email, string password, DateTime utcNow, CancellationToken ct)
    {
        _ = passwordService.VerifyDummy(password);

        await unitOfWork.BeginAsync(ct: ct);
        try
        {
            _ = await accountRepository.FindByEmailForUpdateAsync(email, ct);
            // NoAccountEmail, never `email`: an account registered since the read above must not lose its message.
            _ = await accountRepository.UpdateMessageAsync(NoAccountEmail, null, utcNow, ct);
            await unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }

        logger.LogWarning("Login failed: no account for {Email}", email);
        return LoginResult.Fail(ServiceErrorKeys.EmailOrPasswordWrong);
    }

    /// <summary>The answer to a locked account.</summary>
    private LoginResult RefuseLocked(string email)
    {
        logger.LogWarning("Login refused: account is locked for {Email}", email);
        return LoginResult.Fail(ServiceErrorKeys.AccountLocked);
    }

    /// <summary>
    /// Applies a finished password check to the row as it stands now. The row lock (FOR UPDATE) is held until commit or
    /// rollback, so concurrent attempts against the same account are serialized here: each one sees the latest committed
    /// LoginAttempt / Locked state rather than the unlocked read the check ran on.
    /// </summary>
    /// <param name="email">The normalized address.</param>
    /// <param name="verifiedHash">The hash the password was checked against.</param>
    /// <param name="passwordMatches">Whether it matched.</param>
    /// <param name="utcNow">The instant of this use case.</param>
    /// <param name="ct">Cancellation token.</param>
    private async Task<LoginResult> RecordOutcomeAsync(string email, string verifiedHash, bool passwordMatches, DateTime utcNow, CancellationToken ct)
    {
        await unitOfWork.BeginAsync(ct: ct);
        try
        {
            Account? account = await accountRepository.FindByEmailForUpdateAsync(email, ct);

            if (account is { Locked: true })
            {
                await unitOfWork.RollbackAsync(ct);
                return RefuseLocked(email);
            }

            // The check proved nothing about a password replaced in the meantime, right or wrong, so it is
            // neither a sign-in nor a guess worth counting.
            if (account == null || !string.Equals(account.HashedPassword, verifiedHash, StringComparison.Ordinal))
            {
                await unitOfWork.RollbackAsync(ct);
                logger.LogWarning("Login failed: the password of {Email} changed while it was being verified", email);
                return LoginResult.Fail(ServiceErrorKeys.EmailOrPasswordWrong);
            }

            if (!passwordMatches)
            {
                account.RecordLoginFailure(settings.Value.MaxLoginAttempt, utcNow);
                await accountRepository.UpdateEntityAsync(account, ct);
                await unitOfWork.CommitAsync(ct);
                logger.LogWarning("Login failed: wrong password for {Email} (failed attempts: {LoginAttempts})", email, account.LoginAttempt);
                if (account.Locked)
                    logger.LogWarning("Account {Email} locked after {LoginAttempts} failed login attempts", email, account.LoginAttempt);
                return LoginResult.Fail(ServiceErrorKeys.EmailOrPasswordWrong);
            }

            if (account.CheckSignInOnceAuthenticated() is { } refusal)
            {
                await unitOfWork.RollbackAsync(ct);
                return SignInRefusalResponder.Refuse(logger, refusal, email);
            }

            // Successful login: clear any prior failed attempt counter.
            account.ResetLoginAttempt(utcNow);
            await accountRepository.UpdateEntityAsync(account, ct);
            await unitOfWork.CommitAsync(ct);

            logger.LogInformation("Login succeeded for {Email}", email);
            return LoginResult.Ok(AccountMapper.ToResponse(account));
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}
