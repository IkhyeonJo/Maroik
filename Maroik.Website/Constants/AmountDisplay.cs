using System.Globalization;
using Maroik.Core.Service.Constants;

namespace Maroik.Website.Constants;

/// <summary>
/// How the finance screens show an amount: with a thousands separator, and with its currency's usual decimal places when the
/// currency label is a known ISO 4217 code — none for KRW or JPY ("1,235"), two for USD or EUR ("12.50"), three for KWD — or
/// with every decimal place the column stores (<see cref="PolicyLimits.AmountMaxDecimalPlaces"/>, trailing zeros dropped) when it
/// is not: the label is free text ("$", "원", ...), so an unknown one never hides a digit the user entered. Since 2026-11-14 at the
/// owner's request (before, every currency showed every stored place). Display only: what is stored, validated and calculated is
/// unchanged, and a rounded figure on screen is rounded half away from zero. A presentation setting, so it lives here rather than
/// in the domain (like <c>CulturePolicy</c>).
/// </summary>
public static class AmountDisplay
{
    /// <summary>The .NET custom numeric format for an unknown currency, e.g. <c>"#,0.####"</c>.</summary>
    public static readonly string Format = "#,0." + new string('#', PolicyLimits.AmountMaxDecimalPlaces);

    /// <summary>ISO 4217 currencies without minor units.</summary>
    private static readonly HashSet<string> _noDecimals = new(StringComparer.Ordinal)
    {
        "BIF", "CLP", "DJF", "GNF", "ISK", "JPY", "KMF", "KRW", "PYG", "RWF", "UGX", "UYI", "VND", "VUV", "XAF", "XOF", "XPF"
    };

    /// <summary>ISO 4217 currencies with three decimal places.</summary>
    private static readonly HashSet<string> _threeDecimals = new(StringComparer.Ordinal) { "BHD", "IQD", "JOD", "KWD", "LYD", "OMR", "TND" };

    /// <summary>Widely used ISO 4217 currencies with two decimal places.</summary>
    private static readonly HashSet<string> _twoDecimals = new(StringComparer.Ordinal)
    {
        "AED", "AUD", "BRL", "CAD", "CHF", "CNY", "CZK", "DKK", "EUR", "GBP", "HKD", "HUF", "IDR", "ILS", "INR", "MXN", "MYR", "NOK",
        "NZD", "PHP", "PLN", "RUB", "SAR", "SEK", "SGD", "THB", "TRY", "TWD", "USD", "ZAR"
    };

    /// <summary>The usual decimal places of <paramref name="currency"/> when it is a known code (any case, blanks trimmed), or null.</summary>
    public static int? DecimalPlacesOf(string? currency)
    {
        string code = (currency ?? "").Trim().ToUpperInvariant();
        if (_noDecimals.Contains(code)) return 0;
        if (_twoDecimals.Contains(code)) return 2;
        if (_threeDecimals.Contains(code)) return 3;
        return null;
    }

    /// <summary><paramref name="amount"/> as the screens show it in <paramref name="currency"/>, in the current culture.</summary>
    public static string Text(decimal amount, string? currency) =>
        DecimalPlacesOf(currency) is { } places
            ? amount.ToString(places == 0 ? "#,0" : "#,0." + new string('0', places), CultureInfo.CurrentCulture)
            : amount.ToString(Format, CultureInfo.CurrentCulture);
}
