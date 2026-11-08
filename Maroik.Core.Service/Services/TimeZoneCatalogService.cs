using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.Core.Service.Services;

/// <inheritdoc cref="ITimeZoneCatalogService" />
public class TimeZoneCatalogService : ITimeZoneCatalogService
{
    /// <summary>The option list, built once on first use (the host's zone table does not change at runtime).</summary>
    private readonly Lazy<IReadOnlyList<TimeZoneOptionDto>> _timeZoneOptions = new(BuildTimeZoneOptions);

    /// <inheritdoc />
    public IReadOnlyList<TimeZoneOptionDto> GetTimeZoneOptions() => _timeZoneOptions.Value;

    /// <summary>
    /// Lists the system time zones under their IANA IDs, the form accounts and calendars store. On Linux, where production runs,
    /// every system zone ID already is one. A Windows dev box lists Windows IDs, so those are converted; a Windows zone with no
    /// IANA equivalent is left out, since an option under its Windows ID would match no stored value.
    /// </summary>
    private static List<TimeZoneOptionDto> BuildTimeZoneOptions() =>
    [
        .. TimeZoneInfo.GetSystemTimeZones()
            .Select(systemTimeZone => new { IanaId = FindIanaId(systemTimeZone), systemTimeZone.DisplayName })
            .Where(option => option.IanaId != null)
            .Select(option => new TimeZoneOptionDto
            {
                IanaId = option.IanaId!,
                DisplayName = option.DisplayName
            })
    ];

    /// <summary>The zone's IANA ID: its own ID when it is one, otherwise its conversion, or <see langword="null"/> when it has none.</summary>
    private static string? FindIanaId(TimeZoneInfo systemTimeZone)
    {
        if (systemTimeZone.HasIanaId)
            return systemTimeZone.Id;

        return TimeZoneInfo.TryConvertWindowsIdToIanaId(systemTimeZone.Id, out string? ianaId) ? ianaId : null;
    }
}
