using Maroik.Core.Contract.Dtos;
using Maroik.Website.Contracts;

namespace Maroik.Website.Services;

/// <inheritdoc cref="ITimeZoneCatalogService" />
public class TimeZoneCatalogService : ITimeZoneCatalogService
{
    private readonly Lazy<IReadOnlyList<TimeZoneOptionDto>> _timeZoneOptions = new(BuildTimeZoneOptions);

    /// <inheritdoc />
    public IReadOnlyList<TimeZoneOptionDto> GetTimeZoneOptions() => _timeZoneOptions.Value;

    private static IReadOnlyList<TimeZoneOptionDto> BuildTimeZoneOptions()
    {
        var options = new List<TimeZoneOptionDto>();

        foreach (var systemTimeZone in TimeZoneInfo.GetSystemTimeZones())
        {
            string systemTimeZoneIanaId = string.Empty;
            if (systemTimeZone.HasIanaId)
            {
                systemTimeZoneIanaId = systemTimeZone.Id;
            }
            else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(systemTimeZone.Id, out string? ianaId))
            {
                systemTimeZoneIanaId = ianaId;
            }

            if (string.IsNullOrEmpty(systemTimeZoneIanaId))
            {
                continue;
            }

            options.Add(new TimeZoneOptionDto
            {
                IanaId = systemTimeZoneIanaId,
                DisplayName = systemTimeZone.DisplayName
            });
        }

        return options;
    }
}
