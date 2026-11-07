using Maroik.Core.Contract.Interfaces;
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
    public string FormatForViewer(DateTime utc, bool viewerSeesLocalTime, string timeZoneIanaId) =>
        utc.ToViewerFormattedString(viewerSeesLocalTime, timeZoneIanaId);

    /// <inheritdoc />
    public string GetTimeZoneDisplayName(string timeZoneIanaId) => timeZoneIanaId.ToTimeZoneStandardName();
}
