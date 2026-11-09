namespace Maroik.Core.Contract.Dtos;

/// <summary>
/// Input of the administrator's "create account" use case. Unlike
/// <see cref="RegisterAccountRequest"/> an administrator chooses the role and whether the account
/// starts confirmed.
/// </summary>
public class AdminCreateAccountRequest
{
    /// <summary>Email address used as the primary key / login ID.</summary>
    public string? Email { get; set; }

    /// <summary>
    /// Plain-text password typed by the administrator. The service validates it against the
    /// password policy and hashes it; it is never persisted or returned.
    /// </summary>
    public string? PlainPassword { get; set; }

    /// <summary>Unique display name shown throughout the UI (reserved names are allowed).</summary>
    public string? Nickname { get; set; }

    /// <summary>Account role: "Admin" or "User".</summary>
    public string? Role { get; set; }

    /// <summary>IANA time-zone ID used for date/time display (e.g. "Asia/Seoul").</summary>
    public string? TimeZoneIanaId { get; set; }
}
