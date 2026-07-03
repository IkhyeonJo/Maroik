using Maroik.Website.Extensions;
using Maroik.Website.Models.ViewModels.AccountBook;

namespace Maroik.Website.Tests.Extensions;

/// <summary>Unit tests for <see cref="AccountBookSearchExtensions"/>.</summary>
public class AccountBookSearchExtensionsTests
{
    // ── Asset ─────────────────────────────────────────────────────────────────

    private static List<AssetOutputViewModel> SampleAssets() =>
    [
        new() { ProductName = "Savings",    Item = "Deposit", MonetaryUnit = "KRW", Note = "note1" },
        new() { ProductName = "Stock Fund", Item = "Stock",   MonetaryUnit = "USD", Note = "note2" },
        new() { ProductName = "Cash",       Item = "Cash",    MonetaryUnit = "KRW", Note = ""      }
    ];

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Asset_EmptySearch_ReturnsAll(string? search)
    {
        var result = SampleAssets().ApplySearch(search);
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public void Asset_MatchesProductName()
    {
        var result = SampleAssets().ApplySearch("Sav").ToList();
        Assert.Single(result);
        Assert.Equal("Savings", result[0].ProductName);
    }

    [Fact]
    public void Asset_MatchesMonetaryUnit()
    {
        var result = SampleAssets().ApplySearch("USD").ToList();
        Assert.Single(result);
        Assert.Equal("Stock Fund", result[0].ProductName);
    }

    [Fact]
    public void Asset_NoMatch_ReturnsEmpty()
    {
        var result = SampleAssets().ApplySearch("zzz");
        Assert.Empty(result);
    }

    // ── Income ────────────────────────────────────────────────────────────────

    private static List<IncomeOutputViewModel> SampleIncomes() =>
    [
        new() { MainClass = "RegularIncome",   SubClass = "LaborIncome",    Content = "salary", MonetaryUnit = "KRW" },
        new() { MainClass = "IrregularIncome", SubClass = "FinancialIncome", Content = "dividend", MonetaryUnit = "USD" }
    ];

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Income_EmptySearch_ReturnsAll(string? search)
    {
        var result = SampleIncomes().ApplySearch(search);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public void Income_MatchesContent()
    {
        var result = SampleIncomes().ApplySearch("salary").ToList();
        Assert.Single(result);
        Assert.Equal("salary", result[0].Content);
    }

    [Fact]
    public void Income_MatchesMonetaryUnit()
    {
        var result = SampleIncomes().ApplySearch("USD").ToList();
        Assert.Single(result);
        Assert.Equal("dividend", result[0].Content);
    }

    // ── Expenditure ───────────────────────────────────────────────────────────

    private static List<ExpenditureOutputViewModel> SampleExpenditures() =>
    [
        new() { MainClass = "ConsumerSpending",    SubClass = "MealOrEatOutExpenses", Content = "lunch",  PaymentMethod = "CreditCard" },
        new() { MainClass = "NonConsumerSpending", SubClass = "Tax",                  Content = "income tax", PaymentMethod = "BankTransfer" }
    ];

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Expenditure_EmptySearch_ReturnsAll(string? search)
    {
        var result = SampleExpenditures().ApplySearch(search);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public void Expenditure_MatchesPaymentMethod()
    {
        var result = SampleExpenditures().ApplySearch("Credit").ToList();
        Assert.Single(result);
        Assert.Equal("lunch", result[0].Content);
    }

    [Fact]
    public void Expenditure_MatchesSubClass()
    {
        var result = SampleExpenditures().ApplySearch("Tax").ToList();
        Assert.Single(result);
        Assert.Equal("income tax", result[0].Content);
    }

    [Fact]
    public void Expenditure_NoMatch_ReturnsEmpty()
    {
        var result = SampleExpenditures().ApplySearch("zzz");
        Assert.Empty(result);
    }
}
