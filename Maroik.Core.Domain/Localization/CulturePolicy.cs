namespace Maroik.Core.Domain.Localization;

/// <summary>
/// Locale-derived defaults. Keeps the "which culture implies which default" decisions in one
/// place instead of scattered across views and client scripts.
/// </summary>
public static class CulturePolicy
{
    /// <summary>
    /// The IANA time zone to pre-select on the registration form for <paramref name="culture"/>,
    /// or <see langword="null"/> when the user should pick one explicitly.
    /// </summary>
    public static string? DefaultTimeZoneIanaId(string? culture) => culture switch
    {
        "ko-KR" => "Asia/Seoul",
        _ => null
    };
}
