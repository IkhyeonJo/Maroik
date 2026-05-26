namespace Maroik.Core.Contract.Utilities;

/// <summary>
/// Extension methods for <see cref="decimal"/> to produce clean display strings
/// without unnecessary trailing zeros.
/// </summary>
public static class DecimalExtension
{
    /// <summary>
    /// Returns a string representation of <paramref name="value"/> with trailing decimal zeros removed.
    /// Whole numbers are formatted without a decimal point (e.g. 1000.00 → "1000").
    /// Returns "0" on exception.
    /// </summary>
    public static string TrimTrailingZeros(this decimal value)
    {
        try
        {
            // If the value is a whole number, cast to int to avoid ".0" suffix
            return value % 1 == 0 ? ((int)value).ToString() : value.ToString().TrimEnd('0').TrimEnd('.');
        }
        catch
        {
            return default(decimal).ToString();
        }
    }

    /// <summary>
    /// Nullable overload of <see cref="TrimTrailingZeros(decimal)"/>.
    /// Treats null as zero.
    /// </summary>
    public static string TrimTrailingZeros(this decimal? value)
        => (value ?? 0m).TrimTrailingZeros();
}
