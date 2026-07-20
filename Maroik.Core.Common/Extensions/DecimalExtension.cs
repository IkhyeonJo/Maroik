using System.Globalization;

namespace Maroik.Core.Common.Extensions;

public static class DecimalExtension
{
    public static string TrimTrailingZeros(this decimal value)
    {
        return value.ToString("0.############", CultureInfo.InvariantCulture);
    }

    public static string TrimTrailingZeros(this decimal? value)
    {
        return (value ?? 0m).TrimTrailingZeros();
    }
}