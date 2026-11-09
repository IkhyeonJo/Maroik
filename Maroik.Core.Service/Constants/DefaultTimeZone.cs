using Maroik.Core.Domain.ValueObjects;

namespace Maroik.Core.Service.Constants;

/// <summary>The Domain's default time zone, exposed by the Service layer to the Website, which may not read the Domain.</summary>
public static class DefaultTimeZone
{
    /// <summary><see cref="TimeZoneId.Default"/>.</summary>
    public const string Id = TimeZoneId.Default;
}
