namespace Maroik.Core.Contract.Utilities;

/// <summary>
/// Extension methods for <see cref="string"/> used in the account-book UI.
/// </summary>
public static class StringExtension
{
    /// <summary>
    /// Formats a numeric amount string as a column header label that includes the currency unit,
    /// e.g. "Amount (KRW)". Null values default to an empty string.
    /// </summary>
    /// <param name="value">The base label text (e.g. "Amount").</param>
    /// <param name="monetaryUnit">The currency unit to append (e.g. "KRW", "USD").</param>
    /// <returns>A formatted label string such as "Amount (KRW)".</returns>
    public static string GetAmountLabel(this string value, string monetaryUnit)
    {
        return $"{value ?? ""} ({monetaryUnit ?? ""})";
    }
}
