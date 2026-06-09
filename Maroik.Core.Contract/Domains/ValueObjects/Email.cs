using ErrorOr;
using System.Net.Mail;
using Maroik.Core.Contract.Domains.Primitives;

namespace Maroik.Core.Contract.Domains.ValueObjects;

/// <summary>
/// Value object that represents a validated, normalised email address.
/// Always stored in lower-case.
/// </summary>
public sealed class Email : ValueObject
{
    /// <summary>The normalised (lower-case) email address string.</summary>
    public string Value { get; }

    private Email(string value) => Value = value;

    /// <summary>
    /// Re-creates an <see cref="Email"/> from a value already stored in the database.
    /// Skips validation — only call this when the source is trusted (repository layer).
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

        if (!IsValid(value))
            return Error.Validation("Email.Invalid", $"'{value}' is not a valid email address.");

        return new Email(value.ToLowerInvariant());
    }

    private static bool IsValid(string email)
    {
        if (!MailAddress.TryCreate(email, out MailAddress? address))
            return false;

        string[] hostParts = address.Host.Split('.');
        if (hostParts.Length == 1)
            return false;

        if (hostParts.Any(p => p == string.Empty))
            return false;

        if (hostParts[^1].Length < 2)
            return false;

        if (address.User.Contains(' '))
            return false;

        if (address.User.Split('.').Any(p => p == string.Empty))
            return false;

        return true;
    }

    /// <inheritdoc/>
    public override IEnumerable<object?> GetAtomicValues() { yield return Value; }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
