namespace Maroik.Core.Contract.Interfaces;

/// <summary>Time-zone conversion for showing and reading dates.</summary>
public interface ITimeZoneDisplayService
{
    /// <summary>A UTC value in the time zone; unchanged when the zone is invalid.</summary>
    DateTime ToTimeZone(DateTime utc, string timeZoneIanaId);

    /// <summary>A local value in UTC.</summary>
    DateTime ToUtcFromTimeZone(DateTime local, string timeZoneIanaId);

    /// <summary>A UTC value as a viewer sees it: local time, or ISO-8601 UTC for one who does not see local time.</summary>
    string FormatForViewer(DateTime utc, bool viewerSeesLocalTime, string timeZoneIanaId);

    /// <summary>A time zone's standard name, or the ID itself when it is unknown.</summary>
    string GetTimeZoneDisplayName(string timeZoneIanaId);
}
