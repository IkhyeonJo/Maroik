using Maroik.Core.Contract.Utilities;

namespace Maroik.Core.Contract.Tests.Utilities;

public class StringExtensionTests
{
    // ── GetAmountLabel ───────────────────────────────────────────────────────

    [Fact]
    public void GetAmountLabel_ReturnsFormattedLabel_WithValueAndUnit()
    {
        string result = "Amount".GetAmountLabel("KRW");

        Assert.Equal("Amount (KRW)", result);
    }

    [Fact]
    public void GetAmountLabel_IncludesEmptyParentheses_WhenMonetaryUnitIsEmpty()
    {
        string result = "Amount".GetAmountLabel("");

        Assert.Equal("Amount ()", result);
    }

    [Fact]
    public void GetAmountLabel_IncludesEmptyParentheses_WhenMonetaryUnitIsNull()
    {
        string result = "Amount".GetAmountLabel(null!);

        Assert.Equal("Amount ()", result);
    }

    [Fact]
    public void GetAmountLabel_WorksWithVariousUnits()
    {
        Assert.Equal("잔액 (USD)", "잔액".GetAmountLabel("USD"));
        Assert.Equal("Balance (EUR)", "Balance".GetAmountLabel("EUR"));
        Assert.Equal("금액 (JPY)", "금액".GetAmountLabel("JPY"));
    }
}
