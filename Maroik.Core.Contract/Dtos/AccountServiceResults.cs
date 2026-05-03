namespace Maroik.Core.Contract.Dtos;

/// <summary>
/// Result returned by the login operation.
/// Use the static factory methods <see cref="Ok"/> and <see cref="Fail"/> to create instances.
/// </summary>
public class LoginResult
{
    /// <summary>True when login succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>localization key describing the error (empty string on success).</summary>
    public string ErrorKey { get; init; } = string.Empty;

    /// <summary>The authenticated account data (null on failure).</summary>
    public AccountResponse? Account { get; init; }

    /// <summary>Creates a successful login result carrying the account data.</summary>
    public static LoginResult Ok(AccountResponse account) => new() { Success = true, Account = account };

    /// <summary>Creates a failed login result with the given error key.</summary>
    public static LoginResult Fail(string errorKey) => new() { Success = false, ErrorKey = errorKey };
}

/// <summary>
/// Result returned by the registration operation.
/// Includes flags that tell the UI whether to show a "resend email" prompt.
/// </summary>
public class RegisterResult
{
    /// <summary>True when registration succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>localization key describing the error (empty string on success).</summary>
    public string ErrorKey { get; init; } = string.Empty;

    /// <summary>True when the UI should display the "resend confirmation email" button.</summary>
    public bool ShowResendEmail { get; init; }

    /// <summary>Email address that the confirmation was sent to (used in the UI message).</summary>
    public string? EmailAddress { get; init; }

    /// <summary>True when a duplicate resend was attempted (email was already sent again).</summary>
    public bool RepeatEmailSend { get; init; }

    /// <summary>Creates a successful registration result.</summary>
    public static RegisterResult Ok(bool showResendEmail = false, string? email = null, bool repeat = false)
        => new() { Success = true, ShowResendEmail = showResendEmail, EmailAddress = email, RepeatEmailSend = repeat };

    /// <summary>Creates a failed registration result with the given error key.</summary>
    public static RegisterResult Fail(string errorKey, bool showResendEmail = false, string? email = null)
        => new() { Success = false, ErrorKey = errorKey, ShowResendEmail = showResendEmail, EmailAddress = email };
}

/// <summary>
/// Result returned after the user clicks the email-confirmation link.
/// </summary>
public class ConfirmEmailResult
{
    /// <summary>True when the token was invalid or expired.</summary>
    public bool InvalidToken { get; init; }

    /// <summary>True when the account was successfully activated.</summary>
    public bool AccountCreated { get; init; }

    /// <summary>localization key describing the error (empty string on success).</summary>
    public string ErrorKey { get; init; } = string.Empty;
}

/// <summary>
/// Result returned when validating a password-reset token before allowing the user to set a new password.
/// </summary>
public class ResetPasswordValidationResult
{
    /// <summary>True when validation failed (invalid/expired token).</summary>
    public bool FailToReset { get; init; }

    /// <summary>The validated reset token to pass back during the actual password change.</summary>
    public string? ResetPasswordToken { get; init; }
}
