using ErrorOr;
using Maroik.Core.Contract.Domains.Primitives;

namespace Maroik.Core.Contract.Domains.ValueObjects;

/// <summary>
/// Value object that holds a validated IANA time-zone identifier (e.g. "Asia/Seoul", "UTC").
/// Validation uses <see cref="TimeZoneInfo.FindSystemTimeZoneById"/> which accepts both IANA
/// and Windows zone IDs on .NET 6+.
/// </summary>
public sealed class TimeZoneId : ValueObject
{
    /// <summary>The validated IANA time-zone ID string.</summary>
    public string Value { get; }

    private TimeZoneId(string value) => Value = value;

    /// <summary>
    /// Re-creates a <see cref="TimeZoneId"/> from a value already stored in the database.
    /// Skips validation — only call this when the source is trusted (repository layer).
    /// </summary>
    internal static TimeZoneId FromTrustedSource(string value) => new(value);

    /// <summary>
    /// Creates a <see cref="TimeZoneId"/>, confirming the string maps to a known zone on the host.
    /// Returns a validation error when the ID is null, empty, or unrecognized.
    /// </summary>
    public static ErrorOr<TimeZoneId> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Error.Validation("TimeZoneId.Empty", "Time-zone ID cannot be empty.");

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(value);
        }
        catch (TimeZoneNotFoundException)
        {
            return Error.Validation("TimeZoneId.Invalid", $"'{value}' is not a recognised time-zone ID.");
        }

        return new TimeZoneId(value);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetAtomicValues() { yield return Value; }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
