// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
namespace Maroik.Core.Contract.Dtos;

/// <summary>
/// Result returned by the registration operation.
/// Includes flags that tell the UI whether to show a "resend email" prompt.
/// </summary>
public class RegisterResult
{
    /// <summary>True when registration succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>Localization key describing the error (empty string on success).</summary>
    public string ErrorKey { get; init; } = "";

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
