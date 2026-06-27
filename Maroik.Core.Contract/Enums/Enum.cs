using System.ComponentModel;

namespace Maroik.Core.Contract.Enums;

/// <summary>
/// Result/status messages for account operations.
/// Use <see cref="Maroik.Core.Contract.Helpers.EnumHelper.GetDescription"/> to retrieve the human-readable string.
/// </summary>
public enum AccountMessage
{
    /// <summary>The operation completed without errors.</summary>
    [Description("Success")]
    Success,

    /// <summary>The password-reset operation completed successfully.</summary>
    [Description("Success to reset password")]
    SuccessToResetPassword,

    /// <summary>A generic error occurred during the operation.</summary>
    [Description("Error Found")]
    ErrorFound,

    /// <summary>The email is already registered and the account is active.</summary>
    [Description("User already created, please login")]
    UserAlreadyCreated,

    /// <summary>The email is already registered but not yet confirmed.</summary>
    [Description("User already created, please verify your given mail Id")]
    VerifyEmail,

    /// <summary>No matching account was found for the supplied credentials.</summary>
    [Description("Invalid User, Please Create account")]
    InvalidUser,

    /// <summary>The confirmation or reset email was sent successfully.</summary>
    [Description("Mail Sent")]
    MailSent,

    /// <summary>An error occurred while attempting to send the email.</summary>
    [Description("Fail to mail sent")]
    FailToMailSent,

    /// <summary>A new account was created; the user must click the verification link in the confirmation email.</summary>
    [Description("User created, Check email, click link and verify")]
    UserCreatedVerifyEmail,

    /// <summary>The supplied token is invalid or has expired.</summary>
    [Description("Invalid Token")]
    InvalidToken,

    /// <summary>Resending the confirmation email failed.</summary>
    [Description("Failed to resend email")]
    FailToResendEmail,

    /// <summary>A password-reset email has been dispatched to the supplied address.</summary>
    [Description("Email has been sent to reset password")]
    ResetPasswordMail,

    /// <summary>The account is locked after too many failed login attempts.</summary>
    [Description("This account is locked")]
    AccountLocked
}

/// <summary>
/// Keys used to store data in the server-side session store.
/// </summary>
public enum Session
{
    /// <summary>Session key that holds the serialised <c>AccountResponse</c> for the logged-in user.</summary>
    [Description("Account")]
    Account
}

/// <summary>
/// Identifies the type of a board post, determining which list the post appears in.
/// </summary>
public enum BoardType
{
    /// <summary>Public free-discussion forum visible to all users.</summary>
    [Description("FreeForum")]
    FreeForum,

    /// <summary>Private note visible only to the author.</summary>
    [Description("PrivateNote")]
    PrivateNote
}

/// <summary>
/// Distinguishes between a calendar owned by the current user and one subscribed to from another user.
/// </summary>
public enum CalendarType
{
    /// <summary>Calendar owned by the current user.</summary>
    [Description("My")]
    My,

    /// <summary>Calendar owned by another user that the current user has subscribed to.</summary>
    [Description("Other")]
    Other
}

/// <summary>
/// Selects the RSA signature/encryption algorithm variant used by <c>IRsaService</c>.
/// </summary>
public enum RsaType
{
    /// <summary>RSA with SHA-1 padding. Suitable for shorter key lengths.</summary>
    Rsa = 0,

    /// <summary>RSA with SHA-256 padding. Requires a key length of at least 2048 bits.</summary>
    Rsa2
}
