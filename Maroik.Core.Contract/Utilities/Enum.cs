using System.ComponentModel;

namespace Maroik.Core.Contract.Utilities;

/// <summary>
/// Result/status messages for account operations.
/// Use <see cref="EnumHelper.GetDescription"/> to retrieve the human-readable string.
/// </summary>
public enum AccountMessage
{
    [Description("Success")]
    Success,

    [Description("Success to reset password")]
    SuccessToResetPassword,

    [Description("Error Found")]
    ErrorFound,

    /// <summary>The email is already registered and the account is active.</summary>
    [Description("User already created, please login")]
    UserAlreadyCreated,

    /// <summary>The email is already registered but not yet confirmed.</summary>
    [Description("User already created, please verify your given mail Id")]
    VerifyEmail,

    [Description("Invalid User, Please Create account")]
    InvalidUser,

    [Description("Mail Sent")]
    MailSent,

    [Description("Fail to mail sent")]
    FailToMailSent,

    [Description("User created, Check email, click link and verify")]
    UserCreatedVerifyEmail,

    [Description("Invalid Token")]
    InvalidToken,

    [Description("Failed to resend email")]
    FailToResendEmail,

    [Description("Email has been sent to reset password")]
    ResetPasswordMail,

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
