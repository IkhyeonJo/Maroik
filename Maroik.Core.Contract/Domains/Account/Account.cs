using ErrorOr;
using Maroik.Core.Contract.Domains.Primitives;
using Maroik.Core.Contract.Domains.ValueObjects;
using Maroik.Core.Contract.Utilities;

namespace Maroik.Core.Contract.Domains.Account;

/// <summary>
/// Aggregate root for user accounts.
/// All state changes are performed through domain methods that enforce business rules
/// and raise domain events for cross-cutting concerns (email dispatch, audit, etc.).
/// </summary>
public sealed class Account : AggregateRoot<string>
{
    /// <summary>Validated email address (also serves as the primary key).</summary>
    public Email Email { get; private set; }

    /// <summary>BCrypt-hashed password ?? never exposed as plain text.</summary>
    public string HashedPassword { get; private set; }

    /// <summary>Unique display name shown throughout the UI.</summary>
    public string Nickname { get; private set; }

    /// <summary>Relative path to the user's avatar image.</summary>
    public string? AvatarImagePath { get; private set; }

    /// <summary>Account role: "Admin" or "User".</summary>
    public string Role { get; private set; }

    /// <summary>IANA time-zone used for date/time display.</summary>
    public TimeZoneId TimeZone { get; private set; }

    /// <summary>Default currency code for the personal account-book (e.g. "KRW", "USD"). Null when no currency has been configured.</summary>
    public string? DefaultMonetaryUnit { get; private set; }

    /// <summary>When true the account cannot log in.</summary>
    public bool Locked { get; private set; }

    /// <summary>Number of consecutive failed login attempts.</summary>
    public long LoginAttempt { get; private set; }

    /// <summary>Whether the registration email has been confirmed.</summary>
    public bool EmailConfirmed { get; private set; }

    /// <summary>Whether the user accepted the service terms of use.</summary>
    public bool AgreedServiceTerms { get; private set; }

    /// <summary>Token embedded in the registration confirmation email (valid 24 h). Null after confirmation.</summary>
    public string? RegistrationToken { get; private set; }

    /// <summary>Token embedded in the password-reset email (valid 24 h). Null when no reset is pending.</summary>
    public string? ResetPasswordToken { get; private set; }

    /// <summary>UTC timestamp when the account was created.</summary>
    public DateTime Created { get; private set; }

    /// <summary>UTC timestamp of the most recent update.</summary>
    public DateTime Updated { get; private set; }

    /// <summary>Optional admin-facing note (used for lock reason, etc.).</summary>
    public string? Message { get; private set; }

    /// <summary>Soft-delete flag ?? the row is kept but the account is treated as deleted.</summary>
    public bool Deleted { get; private set; }

    private Account(
        Email email,
        string hashedPassword,
        string nickname,
        string role,
        TimeZoneId timeZone,
        string? defaultMonetaryUnit,
        string? registrationToken,
        bool agreedServiceTerms) : base(email.Value)
    {
        Email = email;
        HashedPassword = hashedPassword;
        Nickname = nickname;
        Role = role;
        TimeZone = timeZone;
        DefaultMonetaryUnit = defaultMonetaryUnit;
        RegistrationToken = registrationToken;
        AgreedServiceTerms = agreedServiceTerms;
        Created = DateTime.UtcNow;
        Updated = DateTime.UtcNow;
    }

    // ------------------------------------------------------------------------
    // Factory / Reconstitution
    // ------------------------------------------------------------------------

    /// <summary>
    /// Rebuilds an <see cref="Account"/> from trusted raw values from the repository layer.
    /// Bypasses validation ?? use only with data from the trusted repository layer.
    /// Does NOT raise domain events.
    /// </summary>
    public static Account Reconstitute(
        string email,
        string hashedPassword,
        string nickname,
        string? avatarImagePath,
        string role,
        string timeZoneIanaId,
        string? defaultMonetaryUnit,
        bool locked,
        long loginAttempt,
        bool emailConfirmed,
        bool agreedServiceTerms,
        string? registrationToken,
        string? resetPasswordToken,
        DateTime created,
        DateTime updated,
        string? message,
        bool deleted)
    {
        var account = new Account(
            Email.FromTrustedSource(email),
            hashedPassword,
            nickname,
            role,
            TimeZoneId.FromTrustedSource(timeZoneIanaId),
            defaultMonetaryUnit,
            registrationToken,
            agreedServiceTerms);

        account.AvatarImagePath = avatarImagePath;
        account.Locked = locked;
        account.LoginAttempt = loginAttempt;
        account.EmailConfirmed = emailConfirmed;
        account.ResetPasswordToken = resetPasswordToken;
        account.Created = created;
        account.Updated = updated;
        account.Message = message;
        account.Deleted = deleted;
        return account;
    }

    /// <summary>
    /// Creates a new <see cref="Account"/> and raises <see cref="AccountCreatedEvent"/>.
    /// </summary>
    public static ErrorOr<Account> Create(
        string emailValue,
        string hashedPassword,
        string nickname,
        string role,
        string timeZoneValue,
        string? defaultMonetaryUnit,
        string? registrationToken,
        bool agreedServiceTerms)
    {
        var emailResult = Email.Create(emailValue);
        if (emailResult.IsError) return emailResult.Errors;

        var tzResult = TimeZoneId.Create(timeZoneValue);
        if (tzResult.IsError) return tzResult.Errors;

        if (string.IsNullOrWhiteSpace(nickname))
            return Error.Validation("Account.NicknameEmpty", "Nickname cannot be empty.");

        if (string.IsNullOrWhiteSpace(hashedPassword))
            return Error.Validation("Account.PasswordEmpty", "Hashed password cannot be empty.");

        var account = new Account(
            emailResult.Value, hashedPassword, nickname, role,
            tzResult.Value, defaultMonetaryUnit, registrationToken, agreedServiceTerms);

        return account;
    }

    // ------------------------------------------------------------------------
    // Domain behaviours
    // ------------------------------------------------------------------------

    /// <summary>
    /// Validates the registration token and activates the account.
    /// Raises <see cref="EmailConfirmedEvent"/> on success.
    /// </summary>
    public ErrorOr<Success> ConfirmEmail(string token)
    {
        if (EmailConfirmed)
            return Error.Conflict("Account.AlreadyConfirmed", "Email address is already confirmed.");

        if (RegistrationToken != token)
            return Error.Validation("Account.InvalidToken", "Invalid email confirmation token.");

        EmailConfirmed = true;
        RegistrationToken = null;
        Updated = DateTime.UtcNow;
        return Result.Success;
    }

    /// <summary>
    /// Stores a password-reset token and raises <see cref="PasswordResetRequestedEvent"/>
    /// so an email can be dispatched by the application layer.
    /// </summary>
    public ErrorOr<Success> RequestPasswordReset(string resetToken)
    {
        if (!EmailConfirmed)
            return Error.Failure("Account.NotConfirmed", "Email must be confirmed before resetting the password.");

        if (Locked)
            return Error.Failure("Account.Locked", "Password reset is not available for a locked account.");

        ResetPasswordToken = resetToken;
        Updated = DateTime.UtcNow;
        return Result.Success;
    }

    /// <summary>
    /// Validates the reset token and replaces the hashed password.
    /// </summary>
    public ErrorOr<Success> ResetPassword(string token, string newHashedPassword)
    {
        if (string.IsNullOrWhiteSpace(ResetPasswordToken) || ResetPasswordToken != token)
            return Error.Validation("Account.InvalidToken", "Invalid or expired password-reset token.");

        if (string.IsNullOrWhiteSpace(newHashedPassword))
            return Error.Validation("Account.PasswordEmpty", "New hashed password cannot be empty.");

        HashedPassword = newHashedPassword;
        ResetPasswordToken = null;
        Updated = DateTime.UtcNow;
        return Result.Success;
    }

    /// <summary>
    /// Increments the failed-login counter and locks the account when <paramref name="maxAttempts"/> is reached.
    /// </summary>
    public void RecordLoginFailure(int maxAttempts)
    {
        LoginAttempt++;
        Updated = DateTime.UtcNow;
        if (LoginAttempt >= maxAttempts)
            Lock(EnumHelper.GetDescription(AccountMessage.AccountLocked));
    }

    /// <summary>Resets the failed-login counter to zero (called after a successful login).</summary>
    public void ResetLoginAttempt()
    {
        LoginAttempt = 0;
        Updated = DateTime.UtcNow;
    }

    /// <summary>
    /// Locks the account, preventing login.
    /// Raises <see cref="AccountLockedEvent"/>.
    /// </summary>
    public void Lock(string? message = null)
    {
        Locked = true;
        Message = message;
        Updated = DateTime.UtcNow;
    }

    /// <summary>Unlocks the account and resets the login-attempt counter.</summary>
    public void Unlock()
    {
        Locked = false;
        LoginAttempt = 0;
        Message = null;
        Updated = DateTime.UtcNow;
    }

    /// <summary>Updates the account's profile fields.</summary>
    public void UpdateProfile(string nickname, string? avatarImagePath, TimeZoneId timeZone, string? defaultMonetaryUnit)
    {
        Nickname = nickname;
        AvatarImagePath = avatarImagePath;
        TimeZone = timeZone;
        DefaultMonetaryUnit = defaultMonetaryUnit;
        Updated = DateTime.UtcNow;
    }

    /// <summary>Replaces the stored BCrypt hash (used after an admin password override).</summary>
    public void ChangePassword(string newHashedPassword)
    {
        HashedPassword = newHashedPassword;
        Updated = DateTime.UtcNow;
    }

    /// <summary>Marks the account as deleted without removing the database row.</summary>
    public void SoftDelete()
    {
        Deleted = true;
        Updated = DateTime.UtcNow;
    }

    /// <summary>Sets the administrative notification message (e.g. lock reason, status text).</summary>
    public void SetMessage(string? message)
    {
        Message = message;
        Updated = DateTime.UtcNow;
    }

    /// <summary>
    /// Assigns a new registration token only when the current token is null or empty.
    /// Used in the resend-confirmation flow when the original token was cleared.
    /// </summary>
    public void RegenerateRegistrationTokenIfEmpty(string newToken)
    {
        if (string.IsNullOrEmpty(RegistrationToken))
        {
            RegistrationToken = newToken;
            Updated = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Admin bypass: confirms the email without token validation.
    /// Use only when an admin creates an account that should be immediately active.
    /// </summary>
    public void ForceConfirmEmail()
    {
        EmailConfirmed = true;
        RegistrationToken = null;
        Updated = DateTime.UtcNow;
    }

    /// <summary>
    /// Ensures the service-terms acceptance flag is set to true.
    /// Used when a returning user tries to register again on an already-confirmed account.
    /// </summary>
    public void AcceptServiceTerms()
    {
        AgreedServiceTerms = true;
        Updated = DateTime.UtcNow;
    }

    /// <summary>
    /// Admin-level composite update. Validates and replaces the time-zone;
    /// resets the login counter whenever the account is unlocked.
    /// </summary>
    public ErrorOr<Success> AdminUpdate(
        string? role,
        string? timeZoneIanaId,
        bool locked,
        long loginAttempt,
        bool emailConfirmed,
        bool agreedServiceTerms,
        string? message,
        bool deleted)
    {
        var tzResult = TimeZoneId.Create(timeZoneIanaId ?? TimeZone.Value);
        if (tzResult.IsError) return tzResult.Errors;

        Role = role ?? Role;
        TimeZone = tzResult.Value;
        Locked = locked;
        LoginAttempt = locked ? loginAttempt : 0;
        EmailConfirmed = emailConfirmed;
        AgreedServiceTerms = agreedServiceTerms;
        Message = message;
        Deleted = deleted;
        Updated = DateTime.UtcNow;
        return Result.Success;
    }
}
