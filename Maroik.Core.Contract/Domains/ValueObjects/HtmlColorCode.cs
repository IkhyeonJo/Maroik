using ErrorOr;
using System.Text.RegularExpressions;
using Maroik.Core.Contract.Domains.Primitives;

namespace Maroik.Core.Contract.Domains.ValueObjects;

/// <summary>
/// Value object that represents a validated HTML hex colour code (e.g. "#FF5733" or "#F53").
/// The stored value is always upper-cased.
/// </summary>
public sealed class HtmlColorCode : ValueObject
{
    private static readonly Regex ColorRegex = new(
        @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$",
        RegexOptions.Compiled);

    /// <summary>The validated HTML hex colour string (upper-case).</summary>
    public string Value { get; }

    private HtmlColorCode(string value) => Value = value;

    /// <summary>
    /// Re-creates an <see cref="HtmlColorCode"/> from a value already stored in the database.
    /// Skips validation ?? only call this when the source is trusted (repository layer).
    /// </summary>
    internal static HtmlColorCode FromTrustedSource(string value) => new(value.ToUpperInvariant());

    /// <summary>
    /// Creates an <see cref="HtmlColorCode"/>, validating the hex format.
    /// Returns a validation error when the value is null, empty, or not a valid hex colour.
    /// </summary>
    public static ErrorOr<HtmlColorCode> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Error.Validation("HtmlColorCode.Empty", "HTML color code cannot be empty.");

        if (!ColorRegex.IsMatch(value))
            return Error.Validation("HtmlColorCode.Invalid", $"'{value}' is not a valid HTML hex color code (e.g. #FF5733).");

        return new HtmlColorCode(value.ToUpperInvariant());
    }

    /// <inheritdoc/>
    public override IEnumerable<object?> GetAtomicValues() { yield return Value; }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
