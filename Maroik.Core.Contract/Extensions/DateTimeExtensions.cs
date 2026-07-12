namespace Maroik.Core.Contract.Extensions;

/// <summary>
/// Extension methods for <see cref="DateTime"/> to simplify time-zone conversions.
/// </summary>
public static class DateTimeExtensions
{
    extension(DateTime value)
    {
        /// <summary>
        /// Converts a <see cref="DateTime"/> value from UTC to the time zone specified by the IANA ID.
        /// Returns the original value unchanged if the time-zone ID is invalid or the conversion fails.
        /// </summary>
        /// <param name="timeZoneIanaId">A valid IANA time-zone ID (e.g. "Asia/Seoul", "America/New_York").</param>
        /// <returns>The converted local date/time, or <paramref name="value"/> on error.</returns>
        public DateTime ConvertTimeByTimeZoneIanaId(string timeZoneIanaId)
        {
            try
            {
                return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(value, timeZoneIanaId);
            }
            catch
            {
                // Return the original value if the IANA ID is unrecognized or conversion fails
                return value;
            }
        }
    }
}
