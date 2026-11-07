using Maroik.Core.Domain.Extensions;

namespace Maroik.Core.Contract.Extensions;

/// <summary>
/// Time-zone conversion for showing and reading dates, for the Website's grids, mappers and views, which may not read the Domain.
/// A plain conversion with no business decision in it; it runs where no service is in reach (a static mapper, a Razor expression),
/// so it stays an extension like the other view helpers of this folder.
/// </summary>
public static class TimeZoneDisplayExtensions
{
    extension(DateTime value)
    {
        /// <summary>This UTC value in the time zone; unchanged when the zone is invalid.</summary>
        public DateTime ToTimeZone(string timeZoneIanaId) => value.ConvertTimeByTimeZoneIanaId(timeZoneIanaId);

        /// <summary>This local value in UTC.</summary>
        public DateTime ToUtcFromTimeZone(string timeZoneIanaId) => value.ConvertToUtcByTimeZoneIanaId(timeZoneIanaId);

        /// <summary>This UTC value as a viewer sees it: local time, or ISO-8601 UTC for one who does not see local time.</summary>
        public string ToViewerText(bool viewerSeesLocalTime, string timeZoneIanaId) =>
            value.ToViewerFormattedString(viewerSeesLocalTime, timeZoneIanaId);
    }

    extension(string timeZoneIanaId)
    {
        /// <summary>The zone's standard name, or the ID itself when it is unknown.</summary>
        public string ToTimeZoneDisplayName() => timeZoneIanaId.ToTimeZoneStandardName();
    }
}
