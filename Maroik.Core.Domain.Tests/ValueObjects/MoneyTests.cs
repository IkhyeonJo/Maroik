using Maroik.Core.Domain.ValueObjects;

namespace Maroik.Core.DomainTests.ValueObjects;

/// <summary>
/// Unit tests for <see cref="Maroik.Core.Domain.ValueObjects.Money"/>.
/// Covers creation, currency normalisation, zero factory, arithmetic (add/subtract),
/// cross-currency exception throwing, and equality.
/// </summary>
public class MoneyTests
{
    /// <summary>Create returns money, when valid.</summary>
    [Fact]
    public void Create_ReturnsMoney_WhenValid()
    {
        var result = Money.Create(1000m, "krw");

        Assert.False(result.IsError);
        Assert.Equal(1000m, result.Value.Amount);
        Assert.Equal("KRW", result.Value.Currency);
    }

    /// <summary>Create returns error, when currency empty.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ReturnsError_WhenCurrencyEmpty(string? currency)
    {
        var result = Money.Create(100m, currency);

        Assert.True(result.IsError);
        Assert.Equal("Money.CurrencyEmpty", result.FirstError.Code);
    }

    /// <summary>Create normalizes to upper case.</summary>
    [Fact]
    public void Create_NormalizesToUpperCase()
    {
        var result = Money.Create(50m, "usd");

        Assert.False(result.IsError);
        Assert.Equal("USD", result.Value.Currency);
    }

    /// <summary>Zero returns zero balance.</summary>
    [Fact]
    public void Zero_ReturnsZeroBalance()
    {
        var money = Money.Zero("KRW");

        Assert.Equal(0m, money.Amount);
        Assert.Equal("KRW", money.Currency);
    }

    /// <summary>Add returns sum money, when same currency.</summary>
    [Fact]
    public void Add_ReturnsSumMoney_WhenSameCurrency()
    {
        var a = Money.Create(1000m, "KRW").Value;
        var b = Money.Create(500m, "KRW").Value;

        var result = a.Add(b);

        Assert.Equal(1500m, result.Amount);
        Assert.Equal("KRW", result.Currency);
    }

    /// <summary>Add throws, when different currency.</summary>
    [Fact]
    public void Add_Throws_WhenDifferentCurrency()
    {
        var krw = Money.Create(1000m, "KRW").Value;
        var usd = Money.Create(10m, "USD").Value;

        Assert.Throws<InvalidOperationException>(() => krw.Add(usd));
    }

    /// <summary>Subtract returns remainder, when same currency.</summary>
    [Fact]
    public void Subtract_ReturnsRemainder_WhenSameCurrency()
    {
        var a = Money.Create(1000m, "KRW").Value;
        var b = Money.Create(300m, "KRW").Value;

        var result = a.Subtract(b);

        Assert.Equal(700m, result.Amount);
        Assert.Equal("KRW", result.Currency);
    }

    /// <summary>Subtract throws, when different currency.</summary>
    [Fact]
    public void Subtract_Throws_WhenDifferentCurrency()
    {
        var krw = Money.Create(1000m, "KRW").Value;
        var usd = Money.Create(10m, "USD").Value;

        Assert.Throws<InvalidOperationException>(() => krw.Subtract(usd));
    }

    /// <summary>Equality same amount and currency are equal.</summary>
    [Fact]
    public void Equality_SameAmountAndCurrency_AreEqual()
    {
        var a = Money.Create(100m, "KRW").Value;
        var b = Money.Create(100m, "KRW").Value;

        Assert.Equal(a, b);
    }

    /// <summary>Equality different amount are not equal.</summary>
    [Fact]
    public void Equality_DifferentAmount_AreNotEqual()
    {
        var a = Money.Create(100m, "KRW").Value;
        var b = Money.Create(200m, "KRW").Value;

        Assert.NotEqual(a, b);
    }
}
