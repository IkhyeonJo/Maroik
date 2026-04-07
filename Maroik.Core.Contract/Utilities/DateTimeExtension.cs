namespace Maroik.Core.Contract.Utilities;

/// <summary>
/// Extension methods for <see cref="DateTime"/> to simplify time-zone conversions.
/// </summary>
public static class DateTimeExtension
{
    /// <summary>
    /// Converts a <see cref="DateTime"/> value from UTC to the time zone specified by the IANA ID.
    /// Returns the original value unchanged if the time-zone ID is invalid or the conversion fails.
    /// </summary>
    /// <param name="value">The UTC date/time to convert.</param>
    /// <param name="timeZoneIanaId">A valid IANA time-zone ID (e.g. "Asia/Seoul", "America/New_York").</param>
    /// <returns>The converted local date/time, or <paramref name="value"/> on error.</returns>
    public static DateTime ConvertTimeByTimeZoneIanaId(this DateTime value, string timeZoneIanaId)
    {
        try
        {
            return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(value, timeZoneIanaId);
        }
        catch
        {
            // Return the original value if the IANA ID is unrecognised or conversion fails
            return value;
        }
    }
}
