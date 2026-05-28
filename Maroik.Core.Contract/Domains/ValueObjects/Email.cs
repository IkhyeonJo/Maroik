using ErrorOr;
using System.Text.RegularExpressions;
using Maroik.Core.Contract.Domains.Primitives;

namespace Maroik.Core.Contract.Domains.ValueObjects;

/// <summary>
/// Value object that represents a validated, normalised email address.
/// Always stored in lower-case.
/// </summary>
public sealed class Email : ValueObject
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>The normalised (lower-case) email address string.</summary>
    public string Value { get; }

    private Email(string value) => Value = value;

    /// <summary>
    /// Re-creates an <see cref="Email"/> from a value already stored in the database.
    /// Skips validation ?? only call this when the source is trusted (repository layer).
    /// </summary>
    internal static Email FromTrustedSource(string value) => new(value.ToLowerInvariant());

    /// <summary>
    /// Creates an <see cref="Email"/> value object, validating format in the process.
    /// Returns a validation error if the value is null, empty, or not a valid email format.
    /// </summary>
    public static ErrorOr<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Error.Validation("Email.Empty", "Email address cannot be empty.");

        if (!EmailRegex.IsMatch(value))
            return Error.Validation("Email.Invalid", $"'{value}' is not a valid email address.");

        return new Email(value.ToLowerInvariant());
    }

    /// <inheritdoc/>
    public override IEnumerable<object?> GetAtomicValues() { yield return Value; }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
