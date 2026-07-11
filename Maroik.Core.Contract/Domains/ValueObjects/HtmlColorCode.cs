using System.Text.RegularExpressions;
using ErrorOr;
using Maroik.Core.Contract.Domains.Primitives;

namespace Maroik.Core.Contract.Domains.ValueObjects;

/// <summary>
/// Value object that represents a validated HTML hex color code (e.g. "#FF5733" or "#F53").
/// The stored value is always upper-cased.
/// </summary>
public sealed partial class HtmlColorCode : ValueObject
{
    private static readonly Regex _colorRegex = MyRegex();

    /// <summary>The validated HTML hex color string (upper-case).</summary>
    public string Value { get; }

    private HtmlColorCode(string value) => Value = value;

    /// <summary>
    /// Re-creates an <see cref="HtmlColorCode"/> from a value already stored in the database.
    /// Skips validation — only call this when the source is trusted (repository layer).
    /// </summary>
    internal static HtmlColorCode FromTrustedSource(string value) => new(value.ToUpperInvariant());

    /// <summary>
    /// Creates an <see cref="HtmlColorCode"/>, validating the hex format.
    /// Returns a validation error when the value is null, empty, or not a valid hex color.
    /// </summary>
    public static ErrorOr<HtmlColorCode> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Error.Validation("HtmlColorCode.Empty", "HTML color code cannot be empty.");

        if (!_colorRegex.IsMatch(value))
            return Error.Validation("HtmlColorCode.Invalid", $"'{value}' is not a valid HTML hex color code (e.g. #FF5733).");

        return new HtmlColorCode(value.ToUpperInvariant());
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetAtomicValues() { yield return Value; }

    /// <inheritdoc/>
    public override string ToString() => Value;
    [GeneratedRegex(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
