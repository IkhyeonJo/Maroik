using System.Globalization;
using Maroik.Core.Domain.Finances;

namespace Maroik.Core.Domain.Tests.Finances;

/// <summary>An income or expenditure amount typed as a negative number is stored as its absolute value.</summary>
public class FinanceStoredAmountTests
{
    /// <summary>The sign is dropped; the digits, and a positive amount or zero, stay as typed.</summary>
    [Theory]
    [InlineData("-1234.5678", "1234.5678")]
    [InlineData("1234.5678", "1234.5678")]
    [InlineData("0", "0")]
    public void ToStoredAmount_DropsTheSign(string typed, string expected)
    {
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture),
            FinanceAmountPolicy.ToStoredAmount(decimal.Parse(typed, CultureInfo.InvariantCulture)));
    }
}
