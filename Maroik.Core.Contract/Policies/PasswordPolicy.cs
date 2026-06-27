using System.Text.RegularExpressions;

namespace Maroik.Core.Contract.Policies;

/// <summary>
/// Centralises the password-complexity rule used across the application.
/// Rule: at least 8 characters, meeting 3 of 4 criteria —
/// uppercase (A-Z), lowercase (a-z), digit (0-9), special character.
/// </summary>
public static class PasswordPolicy
{
    /// <summary>
    /// Regex pattern for the password complexity rule.
    /// Exposed as a <c>const</c> so it can be used directly in
    /// <see cref="System.ComponentModel.DataAnnotations.RegularExpressionAttribute"/> on ViewModels.
    /// </summary>
    public const string Pattern =
        @"^((?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])|(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[^a-zA-Z0-9])|(?=.*?[A-Z])(?=.*?[0-9])(?=.*?[^a-zA-Z0-9])|(?=.*?[a-z])(?=.*?[0-9])(?=.*?[^a-zA-Z0-9])).{8,}$";

    private static readonly Regex CompiledRegex = new(Pattern, RegexOptions.Compiled);

    /// <summary>Returns <c>true</c> when <paramref name="password"/> satisfies the complexity rule.</summary>
    public static bool IsValid(string? password) =>
        !string.IsNullOrEmpty(password) && CompiledRegex.IsMatch(password);
}
