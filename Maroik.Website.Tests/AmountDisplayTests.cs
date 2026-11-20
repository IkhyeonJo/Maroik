using System.Globalization;
using Maroik.Core.Domain.Finances;
using Maroik.Website.Constants;

namespace Maroik.Website.Tests;

/// <summary>Unit tests for <see cref="AmountDisplay"/>.</summary>
public class AmountDisplayTests
{
    /// <summary>
    /// An amount shows every decimal place the column stores (up to <see cref="FinanceAmountPolicy.MaxDecimalPlaces"/>),
    /// without trailing zeros, and with a thousands separator — the same in every supported culture.
    /// </summary>
    [Theory]
    [InlineData("en-US", "10000", "10,000")]
    [InlineData("en-US", "12.5", "12.5")]
    [InlineData("en-US", "12.50", "12.5")]
    [InlineData("en-US", "1.234", "1.234")]
    [InlineData("en-US", "1234.5678", "1,234.5678")]
    [InlineData("en-US", "0.0001", "0.0001")]
    [InlineData("en-US", "0", "0")]
    [InlineData("en-US", "-1234.5", "-1,234.5")]
    [InlineData("ko-KR", "1234.5678", "1,234.5678")]
    [InlineData("ko-KR", "10000", "10,000")]
    public void Format_ShowsUpToTheStoredDecimalPlaces_WithAThousandsSeparator(string culture, string amount, string expected)
        => Assert.Equal(expected, decimal.Parse(amount, CultureInfo.InvariantCulture).ToString(AmountDisplay.Format, new CultureInfo(culture)));

    /// <summary>
    /// A known currency code is shown with its usual decimal places (ISO 4217): none for KRW and JPY, two for USD and EUR, three
    /// for KWD — whatever its letter case or surrounding blanks.
    /// </summary>
    [Theory]
    [InlineData("KRW", 0)]
    [InlineData("JPY", 0)]
    [InlineData("USD", 2)]
    [InlineData("EUR", 2)]
    [InlineData("KWD", 3)]
    [InlineData(" usd ", 2)]
    public void DecimalPlacesOf_AKnownCurrencyCode_IsItsUsualDecimals(string currency, int expected)
    {
        Assert.Equal(expected, AmountDisplay.DecimalPlacesOf(currency));
    }

    /// <summary>A currency written any other way ("$", "원", a typo, nothing) has no known decimals: every stored place is shown.</summary>
    [Theory]
    [InlineData("$")]
    [InlineData("원")]
    [InlineData("XYZ")]
    [InlineData("")]
    [InlineData(null)]
    public void DecimalPlacesOf_AnyOtherLabel_IsUnknown(string? currency)
    {
        Assert.Null(AmountDisplay.DecimalPlacesOf(currency));
    }

    /// <summary>
    /// An amount is shown with its currency's decimal places (rounded half away from zero, trailing zeros kept: 12.5 USD is
    /// "12.50"), or every stored place when the currency is unknown.
    /// </summary>
    [Theory]
    [InlineData("1234.5678", "KRW", "1,235")]
    [InlineData("78533.3333", "KRW", "78,533")]
    [InlineData("1234.5678", "USD", "1,234.57")]
    [InlineData("12.5", "USD", "12.50")]
    [InlineData("0.0005", "KWD", "0.001")]
    [InlineData("1234.5678", "$", "1,234.5678")]
    [InlineData("12.50", null, "12.5")]
    public void Text_UsesTheCurrencyDecimalPlaces(string amount, string? currency, string expected)
    {
        CultureInfo before = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("en-US");
        try
        {
            Assert.Equal(expected, AmountDisplay.Text(decimal.Parse(amount, CultureInfo.InvariantCulture), currency));
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }
    }
}
