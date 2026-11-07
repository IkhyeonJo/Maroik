using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Constants;
using System.ComponentModel.DataAnnotations;

namespace Maroik.Website.Attributes;

/// <summary>
/// ViewModel mirror of <c>PasswordPolicy.IsWithinMaxByteLength</c>: a password may be at most
/// <see cref="PolicyLimits.PasswordMaxByteLength"/> UTF-8 bytes (a character count cannot express that — one
/// Korean character is 3 bytes). Authoritative in <c>PasswordPolicy</c> (the services re-validate);
/// having it on the ViewModel lets the form report "too long" instead of a generic failure.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PasswordMaxBytesAttribute : ValidationAttribute
{
    /// <inheritdoc />
    public override bool RequiresValidationContext => true;

    /// <inheritdoc />
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) =>
        value is not string password || validationContext.GetRequiredService<IPolicyService>().IsPasswordWithinMaxByteLength(password)
            ? ValidationResult.Success
            : new ValidationResult(FormatErrorMessage(validationContext.DisplayName), validationContext.MemberName is { } member ? [member] : null);
}
