using System.Globalization;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Calendars;
using Maroik.Core.Domain.Extensions;

namespace Maroik.Core.Service.Services;

/// <inheritdoc cref="ITimeZoneDisplayService"/>
public sealed class TimeZoneDisplayService : ITimeZoneDisplayService
{
    /// <inheritdoc />
    public DateTime ToTimeZone(DateTime utc, string timeZoneIanaId) => utc.ConvertTimeByTimeZoneIanaId(timeZoneIanaId);

    /// <inheritdoc />
    public DateTime ToUtcFromTimeZone(DateTime local, string timeZoneIanaId) => local.ConvertToUtcByTimeZoneIanaId(timeZoneIanaId);

    /// <inheritdoc />
    public DateTime ToAllDayInstant(int year, int month, int day) => CalendarEvent.AllDayInstant(year, month, day);

    /// <inheritdoc />
    public string FormatForViewer(DateTime utc, bool viewerSeesLocalTime, string timeZoneIanaId) =>
        viewerSeesLocalTime
            ? utc.ConvertTimeByTimeZoneIanaId(timeZoneIanaId).ToString(CultureInfo.InvariantCulture)
            : utc.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public string GetTimeZoneDisplayName(string timeZoneIanaId) => timeZoneIanaId.ToTimeZoneStandardName();
}
