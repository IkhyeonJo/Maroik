namespace Maroik.Core.Contract.Dtos;

/// <summary>
/// Data transfer object returned when reading account information.
/// Mirrors AccountRequest but is used exclusively for output (read) operations.
/// </summary>
public class AccountResponse
{
    /// <summary>Email address (primary key / login ID).</summary>
    public string? Email { get; set; }

    /// <summary>Unique display name shown in the UI.</summary>
    public string? Nickname { get; set; }

    /// <summary>Relative path to the user's avatar image.</summary>
    public string? AvatarImagePath { get; set; }

    /// <summary>Account role: "Admin" or "User".</summary>
    public string? Role { get; set; }

    /// <summary>IANA time-zone ID (e.g. "Asia/Seoul", "UTC").</summary>
    public string? TimeZoneIanaId { get; set; }

    /// <summary>Default currency unit for the account-book (e.g. "KRW", "USD").</summary>
    public string? DefaultMonetaryUnit { get; set; }

    /// <summary>Whether the account is locked (cannot log in).</summary>
    public bool Locked { get; set; }

    /// <summary>Number of consecutive failed login attempts.</summary>
    public long LoginAttempt { get; set; }

    /// <summary>Whether the email address has been verified.</summary>
    public bool EmailConfirmed { get; set; }

    /// <summary>Whether the user agreed to the service terms of use.</summary>
    public bool AgreedServiceTerms { get; set; }

    /// <summary>UTC timestamp when the account was created.</summary>
    public DateTime Created { get; set; }

    /// <summary>UTC timestamp of the most recent account update.</summary>
    public DateTime Updated { get; set; }

    /// <summary>Optional admin note attached to the account.</summary>
    public string? Message { get; set; }

    /// <summary>Soft-delete flag.</summary>
    public bool Deleted { get; set; }

    /// <summary>Hashed password (cleared before storing in session; admin-only field).</summary>
    public string? HashedPassword { get; set; }

    /// <summary>One-time token used to confirm the registration e-mail.</summary>
    public string? RegistrationToken { get; set; }

    /// <summary>One-time token used to authorise a password-reset request.</summary>
    public string? ResetPasswordToken { get; set; }
}
