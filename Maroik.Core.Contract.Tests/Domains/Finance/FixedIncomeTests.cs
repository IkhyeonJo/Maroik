using Maroik.Core.Contract.Domains.Finance;

namespace Maroik.Core.Contract.Tests.Domains.Finance;

public class FixedIncomeTests
{
    private static readonly DateTime Maturity = DateTime.UtcNow.AddYears(1);

    private static FixedIncome ValidFixedIncome() =>
        FixedIncome.Register(1, "user@example.com", "RegularIncome", "LaborIncome", "Salary",
            3000000m, "KRW", "My Bank", 0, 25, Maturity).Value;

    // ── Register ─────────────────────────────────────────────────────────────

    [Fact]
    public void Register_ReturnsFixedIncome_WhenValid()
    {
        var result = FixedIncome.Register(1, "user@example.com", "RegularIncome", "LaborIncome", "Salary",
            3000000m, "KRW", "My Bank", 0, 25, Maturity);

        Assert.False(result.IsError);
        Assert.Equal(25, result.Value.DepositDay);
        Assert.Equal(0, result.Value.DepositMonth);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void Register_ReturnsError_WhenDepositDayOutOfRange(short day)
    {
        var result = FixedIncome.Register(1, "user@example.com", "RegularIncome", "LaborIncome", null,
            0m, "KRW", "My Bank", 0, day, Maturity);

        Assert.True(result.IsError);
        Assert.Equal("FixedIncome.InvalidDepositDay", result.FirstError.Code);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(13)]
    public void Register_ReturnsError_WhenDepositMonthOutOfRange(short month)
    {
        var result = FixedIncome.Register(1, "user@example.com", "RegularIncome", "LaborIncome", null,
            0m, "KRW", "My Bank", month, 1, Maturity);

        Assert.True(result.IsError);
        Assert.Equal("FixedIncome.InvalidDepositMonth", result.FirstError.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Register_ReturnsError_WhenMainClassEmpty(string? mainClass)
    {
        var result = FixedIncome.Register(1, "user@example.com", mainClass, "LaborIncome", null,
            0m, "KRW", "My Bank", 0, 1, Maturity);

        Assert.True(result.IsError);
        Assert.Equal("FixedIncome.MainClassEmpty", result.FirstError.Code);
    }

    // ── MarkUnpunctual / ClearUnpunctuality ───────────────────────────────────

    [Fact]
    public void MarkUnpunctual_SetsFlag()
    {
        var fi = ValidFixedIncome();

        fi.MarkUnpunctual();

        Assert.True(fi.Unpunctuality);
    }

    [Fact]
    public void ClearUnpunctuality_ClearsFlag()
    {
        var fi = ValidFixedIncome();
        fi.MarkUnpunctual();

        fi.ClearUnpunctuality();

        Assert.False(fi.Unpunctuality);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public void Update_Succeeds_WhenValid()
    {
        var fi = ValidFixedIncome();

        var result = fi.Update("RegularIncome", "PensionIncome", "Pension", 500000m, "KRW", "Pension Account", 1, 10, Maturity, "note");

        Assert.False(result.IsError);
        Assert.Equal("PensionIncome", fi.SubClass);
        Assert.Equal(10, fi.DepositDay);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void Update_ReturnsError_WhenDepositDayOutOfRange(short day)
    {
        var fi = ValidFixedIncome();

        var result = fi.Update("RegularIncome", "LaborIncome", null, 0m, "KRW", "My Bank", 0, day, Maturity, null);

        Assert.True(result.IsError);
        Assert.Equal("FixedIncome.InvalidDepositDay", result.FirstError.Code);
    }
}
