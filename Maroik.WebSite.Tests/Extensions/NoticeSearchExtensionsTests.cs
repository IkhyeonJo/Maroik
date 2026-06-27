using Maroik.WebSite.Extensions;
using Maroik.WebSite.Models.ViewModels.Notice;

namespace Maroik.WebSite.Tests.Extensions;

/// <summary>Unit tests for <see cref="NoticeSearchExtensions"/>.</summary>
public class NoticeSearchExtensionsTests
{
    // ── FixedIncome ───────────────────────────────────────────────────────────

    private static List<FixedIncomeOutputViewModel> SampleIncomes() =>
    [
        new() { MainClass = "RegularIncome",   SubClass = "LaborIncome",    Content = "salary",  MonetaryUnit = "KRW", DepositMonth = 1, DepositDay = 25, MaturityDate = "2030-12-31" },
        new() { MainClass = "IrregularIncome", SubClass = "OtherIncome",    Content = "bonus",   MonetaryUnit = "USD", DepositMonth = 6, DepositDay = 15, MaturityDate = "2025-06-15" }
    ];

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void FixedIncome_EmptySearch_ReturnsAll(string? search)
        => Assert.Equal(2, SampleIncomes().ApplySearch(search).Count());

    [Fact]
    public void FixedIncome_MatchesContent()
    {
        var result = SampleIncomes().ApplySearch("salary").ToList();
        Assert.Single(result);
        Assert.Equal("salary", result[0].Content);
    }

    [Fact]
    public void FixedIncome_MatchesMonetaryUnit()
    {
        var result = SampleIncomes().ApplySearch("USD").ToList();
        Assert.Single(result);
        Assert.Equal("bonus", result[0].Content);
    }

    [Fact]
    public void FixedIncome_NoMatch_ReturnsEmpty()
        => Assert.Empty(SampleIncomes().ApplySearch("zzz"));

    // ── FixedExpenditure ──────────────────────────────────────────────────────

    private static List<FixedExpenditureOutputViewModel> SampleExpenditures() =>
    [
        new() { MainClass = "ConsumerSpending",    SubClass = "ProtectionTypeInsurance", Content = "insurance", PaymentMethod = "Card",  MonetaryUnit = "KRW", DepositMonth = 1, DepositDay = 5,  MaturityDate = "2030-12-31" },
        new() { MainClass = "NonConsumerSpending", SubClass = "Tax",                     Content = "tax",       PaymentMethod = "Wire",  MonetaryUnit = "USD", DepositMonth = 5, DepositDay = 31, MaturityDate = "2026-01-01" }
    ];

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void FixedExpenditure_EmptySearch_ReturnsAll(string? search)
        => Assert.Equal(2, SampleExpenditures().ApplySearch(search).Count());

    [Fact]
    public void FixedExpenditure_MatchesPaymentMethod()
    {
        var result = SampleExpenditures().ApplySearch("Card").ToList();
        Assert.Single(result);
        Assert.Equal("insurance", result[0].Content);
    }

    [Fact]
    public void FixedExpenditure_NoMatch_ReturnsEmpty()
        => Assert.Empty(SampleExpenditures().ApplySearch("zzz"));
}
