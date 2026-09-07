using System.Globalization;
using Maroik.Core.Domain.Account;

namespace Maroik.Core.Contract.Misc.Extensions;

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
                // The source value is always UTC regardless of its DateTimeKind (e.g. Npgsql
                // returns Unspecified for "timestamp without time zone" columns), so the source
                // must be pinned to UTC explicitly rather than relying on ConvertTimeBySystemTimeZoneId's
                // Kind-based inference, which would otherwise silently fall back to the OS local zone.
                TimeZoneInfo targetZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneIanaId);
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(value, DateTimeKind.Utc), targetZone);
            }
            catch
            {
                // Return the original value if the IANA ID is unrecognized or conversion fails
                return value;
            }
        }

        /// <summary>
        /// Formats a UTC timestamp for display based on the viewer's role: logged-in Admin/User viewers
        /// see it converted to their local time zone, Anonymous viewers see the raw UTC value in
        /// ISO-8601 ("yyyy-MM-ddTHH:mm:ssZ") form.
        /// </summary>
        public string ToViewerFormattedString(string? viewerRole, string timeZoneIanaId) =>
            viewerRole is Role.Admin or Role.User
                ? value.ConvertTimeByTimeZoneIanaId(timeZoneIanaId).ToString(CultureInfo.InvariantCulture)
                : value.ToString("yyyy-MM-ddTHH:mm:ssZ");

        /// <summary>
        /// Converts a local (<see cref="DateTimeKind.Unspecified"/>) <see cref="DateTime"/> value to UTC,
        /// treating it as wall-clock time in the time zone specified by the IANA ID.
        /// Returns the original value unchanged if the time-zone ID is invalid or the conversion fails.
        /// The inverse of <see cref="ConvertTimeByTimeZoneIanaId"/>.
        /// </summary>
        /// <param name="timeZoneIanaId">A valid IANA time-zone ID (e.g. "Asia/Seoul", "America/New_York").</param>
        public DateTime ConvertToUtcByTimeZoneIanaId(string timeZoneIanaId)
        {
            try
            {
                TimeZoneInfo sourceZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneIanaId);
                return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(value, DateTimeKind.Unspecified), sourceZone);
            }
            catch
            {
                // Return the original value if the IANA ID is unrecognized or conversion fails
                return value;
            }
        }
    }

    extension(string timeZoneIanaId)
    {
        /// <summary>
        /// Returns the <see cref="TimeZoneInfo.StandardName"/> for an IANA time-zone ID
        /// (e.g. "Asia/Seoul" → "Korea Standard Time").
        /// Falls back to the raw IANA ID string if the ID is invalid or unrecognized.
        /// </summary>
        public string ToTimeZoneStandardName()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneIanaId).StandardName;
            }
            catch
            {
                return timeZoneIanaId;
            }
        }
    }
}
